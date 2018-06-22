using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MSTTS
{

    public partial class NativeConstants
    {

        /// INCLUDE_UNVDDEF_H_ -> 
        /// Error generating expression: 值不能为空。
        ///参数名: node
        public const string INCLUDE_UNVDDEF_H_ = "";

        /// UNVDAPI -> 
        /// Error generating expression: 值不能为空。
        ///参数名: node
        public const string UNVDAPI = "";

        /// IN_PARAM -> 
        /// Error generating expression: 值不能为空。
        ///参数名: node
        public const string IN_PARAM = "";

        /// OUT_PARAM -> 
        /// Error generating expression: 值不能为空。
        ///参数名: node
        public const string OUT_PARAM = "";
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  

    public partial class NativeMethods
    {

        /// Return Type: boolean
        [System.Runtime.InteropServices.DllImportAttribute("ffmpeg_insertAudio.dll", EntryPoint = "InsertBlankAudio", CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall)]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.I4)]
        public static extern int InsertBlankAudio(string filepathOut, string filepathIn, int nFrontPackCnt, int nTailPackCnt);



    }

}
