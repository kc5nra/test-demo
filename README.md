## Install
Build libobs yourself or use this [prebuilt version 27.5.32](https://obsstudios3.streamlabs.com/libobs-windows64-release-27.5.32.7z) provided by Streamlabs.

If you are using the prebuilt version, this is what the file structure should (roughly) look like after you unzip:
```
- packed_build
    - bin
        - 64bit
            - obs.dll & ~dependencies/.dlls, etc. files~
    - cmake
    - data
    - include
    - obs-plugins
```

Using the `obs_net.example` project as an example, this is how the libobs files should be located under `Debug` folder in order for everything to work correctly when debugging.

```
- Debug
    - net7.0
        - data
        - obs-plugins
        - obs.dll & ~dependencies/.dlls, etc. files~
        - obs_net.example.exe
```
