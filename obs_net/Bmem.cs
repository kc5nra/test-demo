using System.Runtime.InteropServices;

namespace libobs {
    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern long bnum_allocs();
    }
}
