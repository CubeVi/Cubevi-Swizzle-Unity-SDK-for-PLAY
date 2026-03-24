using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
namespace Cubevi_Swizzle
{
    /// <summary>
    /// Tool: Windows system folder/file selection window
    /// </summary>
    public class OpenFile
    {
        public static string ChooseVideo()
        {
            OpenFileName openFileName = new OpenFileName();
            openFileName.structSize = Marshal.SizeOf(openFileName);

            // Update filter to include video file types
            openFileName.file = "Video Files\0*.mp4;*.avi;*.mkv;*.flv;*.wmv;*.webm;*.ts;*.mts;*.mpg;*.vob;*.ogv;*.3gp;*.rmvb;*.asf\0All Files\0*.*\0";

            openFileName.file = new string(new char[256]);
            openFileName.maxFile = openFileName.file.Length;
            openFileName.fileTitle = new string(new char[64]);
            openFileName.maxFileTitle = openFileName.fileTitle.Length;
            openFileName.initialDir = Application.dataPath; // Default path set to project's Assets directory
            openFileName.title = "Select Video File";
            openFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

            if (WindowDll.GetOpenFileName(openFileName))
            {
                string path = openFileName.file.TrimEnd('\0');
                return path;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Select folder
        /// </summary>
        public static string ChooseWinFolder()
        {
            OpenDialogDir ofn = new OpenDialogDir();
            ofn.pszDisplayName = new string(new char[2000]); ;     // Buffer for directory path
            ofn.title = "Select Folder";
            IntPtr pidlPtr = WindowDll.SHBrowseForFolder(ofn);

            char[] charArray = new char[2000];
            for (int i = 0; i < 2000; i++)
                charArray[i] = '\0';

            WindowDll.SHGetPathFromIDList(pidlPtr, charArray);
            string fullDirPath = new String(charArray);
            return fullDirPath.Substring(0, fullDirPath.IndexOf('\0'));
        }

        /// <summary>
        /// Select application file
        /// </summary>
        public static string ChooseWinFile()
        {
            OpenFileName OpenFileName = new OpenFileName();
            OpenFileName.structSize = Marshal.SizeOf(OpenFileName);
            OpenFileName.filter = "Application (*.exe)\0*.exe";
            OpenFileName.file = new string(new char[1024]);
            OpenFileName.maxFile = OpenFileName.file.Length;
            OpenFileName.fileTitle = new string(new char[64]);
            OpenFileName.maxFileTitle = OpenFileName.fileTitle.Length;
            OpenFileName.title = "Select exe file";
            OpenFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            if (WindowDll.GetOpenFileName(OpenFileName))
                return OpenFileName.file;
            else
                return null;
        }

        /// <summary>
        /// Select image file
        /// </summary>
        public static string ChooseImageFile()
        {
            OpenFileName OpenFileName = new OpenFileName();
            OpenFileName.structSize = Marshal.SizeOf(OpenFileName);
            OpenFileName.filter = "Image Files (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg";
            OpenFileName.file = new string(new char[1024]);
            OpenFileName.maxFile = OpenFileName.file.Length;
            OpenFileName.fileTitle = new string(new char[64]);
            OpenFileName.maxFileTitle = OpenFileName.fileTitle.Length;
            OpenFileName.title = "Select Image File";
            OpenFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            if (WindowDll.GetOpenFileName(OpenFileName))
                return OpenFileName.file;
            else
                return null;
        }
    }

    /// <summary>
    /// Windows system file selection window
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct OpenFileName
    {
        public int structSize;
        public IntPtr dlgOwner;
        public IntPtr instance;
        public String filter;
        public String customFilter;
        public int maxCustFilter;
        public int filterIndex;
        public String file;
        public int maxFile;
        public String fileTitle;
        public int maxFileTitle;
        public String initialDir;
        public String title;
        public int flags;
        public short fileOffset;
        public short fileExtension;
        public String defExt;
        public IntPtr custData;
        public IntPtr hook;
        public String templateName;
        public IntPtr reservedPtr;
        public int reservedInt;
        public int flagsEx;
    }

    /// <summary>
    /// Windows system folder selection window
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct OpenDialogDir
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public String pszDisplayName;
        public String title;
        public UInt32 ulFlags;
        public IntPtr lpfno;
        public IntPtr lParam;
        public int iImage;
    }

    public class WindowDll
    {
        // Link specific system function: Open file dialog
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
        public static bool GetOFN([In, Out] OpenFileName ofn)
        {
            return GetOpenFileName(ofn);
        }

        // Link specific system function: Save as dialog
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);
        public static bool GetSFN([In, Out] OpenFileName ofn)
        {
            return GetSaveFileName(ofn);
        }

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder([In, Out] OpenDialogDir ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList([In] IntPtr pidl, [In, Out] char[] fileName);
    }
}
