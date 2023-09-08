using static libobs.Obs;
using libobs;
using System.Runtime.InteropServices;

namespace dm_demo
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Location = new Point(12, 12);
            panel1.Name = "panel1";
            panel1.Size = new Size(776, 426);
            panel1.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);

            Init(new string[] { });
        }

        #endregion

        private Panel panel1;

        internal sealed class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern bool AllocConsole();

            [DllImport("kernel32.dll")]
            public static extern bool FreeConsole();
        }

        void Init(string[] args)
        {
            // Only call this once
            if (obs_initialized())
            {
                throw new Exception("error: obs already initialized");
            }

            // Setup log handler override
            base_set_log_handler(new log_handler_t((lvl, msg, args, p) =>
            {
                using (va_list arglist = new va_list(args))
                {
                    object[] objs = arglist.GetObjectsByFormat(msg);
                    string formattedMsg = Printf.sprintf(msg, objs);

                    Console.WriteLine(((LogErrorLevel)lvl).ToString() + ": " + formattedMsg);
                }
            }), IntPtr.Zero);

            NativeMethods.AllocConsole();

            // Startup obs runtime
            Console.WriteLine("libobs version: " + obs_get_version_string());
            if (!obs_startup("en-US", null, IntPtr.Zero))
            {
                throw new Exception("error on libobs startup");
            }

            // Setup all the dependency locations
            obs_add_data_path("./data/libobs/");
            obs_add_module_path("./obs-plugins/64bit/", "./data/obs-plugins/%module%/");
            
            // Load all plugins
            obs_load_all_modules();
            obs_log_loaded_modules();

            // Setup audio mixer
            obs_audio_info avi = new()
            {
                samples_per_sec = 44100,
                speakers = speaker_layout.SPEAKERS_STEREO
            };
            bool resetAudioCode = obs_reset_audio(ref avi);

            // Canvas size
            int canvasWidth = 1920;
            int canvasHeight = 1080;

            // Setup video mixer
            obs_video_info ovi = new()
            {
                adapter = 0,
                graphics_module = "libobs-d3d11",
                fps_num = 60,
                fps_den = 1,
                base_width = (uint)canvasWidth,
                base_height = (uint)canvasHeight,
                output_width = (uint)canvasWidth,
                output_height = (uint)canvasHeight,
                output_format = video_format.VIDEO_FORMAT_NV12,
                gpu_conversion = true,
                colorspace = video_colorspace.VIDEO_CS_DEFAULT,
                range = video_range_type.VIDEO_RANGE_DEFAULT,
                scale_type = obs_scale_type.OBS_SCALE_BILINEAR
            };
            int resetVideoCode = obs_reset_video(ref ovi);
            if (resetVideoCode != 0)
            {
                throw new Exception("error on libobs reset video: " + ((VideoResetError)resetVideoCode).ToString());
            }

            obs_post_load_modules();

            // Create display/monitor capture
            IntPtr videoSource = obs_source_create("monitor_capture", "Monitor Capture Source", IntPtr.Zero, IntPtr.Zero);
            obs_set_output_source(0, videoSource);

            
            // Setup desktop audio capture
            IntPtr audioSource = obs_source_create("wasapi_output_capture", "Audio Capture Source", IntPtr.Zero, IntPtr.Zero);
            obs_set_output_source(1, audioSource);

            // Setup video encoder: x264 (software)
            IntPtr videoEncoderSettings = obs_data_create();
            obs_data_set_bool(videoEncoderSettings, "use_bufsize", true);
            obs_data_set_string(videoEncoderSettings, "profile", "high");
            obs_data_set_string(videoEncoderSettings, "preset", "veryfast");
            obs_data_set_string(videoEncoderSettings, "rate_control", "CRF");
            obs_data_set_int(videoEncoderSettings, "crf", 20);
            IntPtr videoEncoder = obs_video_encoder_create("obs_x264", "simple_h264_recording", videoEncoderSettings, IntPtr.Zero);
            obs_encoder_set_video(videoEncoder, obs_get_video());
            obs_data_release(videoEncoderSettings);

            // Setup audio encoder: AAC (software)
            IntPtr audioEncoder = obs_audio_encoder_create("ffmpeg_aac", "simple_aac_recording", IntPtr.Zero, (UIntPtr)0, IntPtr.Zero);
            obs_encoder_set_audio(audioEncoder, obs_get_audio());

            // Create recording output
            IntPtr recordOutputSettings = obs_data_create();
            obs_data_set_string(recordOutputSettings, "path", "./dm-demo.mp4");
            IntPtr recordOutput = obs_output_create("ffmpeg_muxer", "simple_ffmpeg_output", recordOutputSettings, IntPtr.Zero);
            obs_data_release(recordOutputSettings);

            // Set encoders to recording output
            obs_output_set_video_encoder(recordOutput, videoEncoder);
            obs_output_set_audio_encoder(recordOutput, audioEncoder, (UIntPtr)0);

            // Start recordnig output
            bool recordOutputStartSuccess = obs_output_start(recordOutput);
            Console.WriteLine("record output successful start: " + recordOutputStartSuccess);
            if (recordOutputStartSuccess != true)
            {
                MessageBox.Show("record output error: '" + obs_output_get_last_error(recordOutput) + "'");
            }

            // Initialize preview
            gs_init_data init_data = new()
            {
                window = panel1.Handle,
                cx = (uint)panel1.Width,
                cy = (uint)panel1.Height,
                format = gs_color_format.GS_BGRA,
                zsformat = gs_zstencil_format.GS_ZS_NONE,
                num_backbuffers = 0,
            };

            // Create display
            IntPtr display = obs_display_create(init_data, 0);

            // Handle resize events and call obs_display resize to adjust window handle size
            //obs_display_resize(display, (uint)panel1.Width, (uint)panel1.Height);

            obs_display_add_draw_callback(display, (data, cx, cy) =>
            {
                // Do math/GS here to scale main preview texture
                obs_render_main_texture();
            }, 0);
        }
    }
}