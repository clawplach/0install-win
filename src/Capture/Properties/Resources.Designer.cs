﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.225
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ZeroInstall.Capture.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ZeroInstall.Capture.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The directory &apos;{0}&apos; is not empty..
        /// </summary>
        internal static string DirectoryNotEmpty {
            get {
                return ResourceManager.GetString("DirectoryNotEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No 32-bit %ProgramFiles% directory found..
        /// </summary>
        internal static string MissingProgramFiles32Bit {
            get {
                return ResourceManager.GetString("MissingProgramFiles32Bit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Multiple installation directories were detected. Choosing first by default..
        /// </summary>
        internal static string MultipleInstallationDirsDetected {
            get {
                return ResourceManager.GetString("MultipleInstallationDirsDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No installation directory was detected..
        /// </summary>
        internal static string NoInstallationDirDetected {
            get {
                return ResourceManager.GetString("NoInstallationDirDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The directory &apos;{0}&apos; is not a capture directory. Please use &apos;0capture init&apos;..
        /// </summary>
        internal static string NotCaptureDirectory {
            get {
                return ResourceManager.GetString("NotCaptureDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method is currently only available on Windows..
        /// </summary>
        internal static string OnlyAvailableOnWindows {
            get {
                return ResourceManager.GetString("OnlyAvailableOnWindows", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Using &apos;{0}&apos; as installation directory..
        /// </summary>
        internal static string UsingInstallationDir {
            get {
                return ResourceManager.GetString("UsingInstallationDir", resourceCulture);
            }
        }
    }
}
