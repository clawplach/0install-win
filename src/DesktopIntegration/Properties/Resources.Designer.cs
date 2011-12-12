﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ZeroInstall.DesktopIntegration.Properties {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ZeroInstall.DesktopIntegration.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to The access point &apos;{0}&apos; for the application &apos;{1}&apos; is in conflict with the new access point &apos;{2}&apos; for the application &apos;{3}&apos;..
        /// </summary>
        internal static string AccessPointConflict {
            get {
                return ResourceManager.GetString("AccessPointConflict", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The application &apos;{0}&apos; is already in the application list..
        /// </summary>
        internal static string AppAlreadyInList {
            get {
                return ResourceManager.GetString("AppAlreadyInList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The application &apos;{0}&apos; is not in the application list..
        /// </summary>
        internal static string AppNotInList {
            get {
                return ResourceManager.GetString("AppNotInList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can only perform one desktop integration operation at a time..
        /// </summary>
        internal static string IntegrationMutex {
            get {
                return ResourceManager.GetString("IntegrationMutex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The name &apos;{0}&apos; contains invalid characters..
        /// </summary>
        internal static string NameInvalidChars {
            get {
                return ResourceManager.GetString("NameInvalidChars", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was a problem loading &apos;{0}&apos;..
        /// </summary>
        internal static string ProblemLoading {
            get {
                return ResourceManager.GetString("ProblemLoading", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The shortcut name &apos;{0}&apos; contains invalid characters..
        /// </summary>
        internal static string ShortcutNameInvalidChars {
            get {
                return ResourceManager.GetString("ShortcutNameInvalidChars", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The username and/or password you entered for the Sync server are incorrect. Please check your Synchronization Options..
        /// </summary>
        internal static string SyncCredentialsInvalid {
            get {
                return ResourceManager.GetString("SyncCredentialsInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Cypto key you entered for the Sync server is incorrect. Please check your Synchronization Options.
        ///You can change the Crypto key used on the server by running &apos;0install sync --reset=server&apos;..
        /// </summary>
        internal static string SyncCryptoKeyInvalid {
            get {
                return ResourceManager.GetString("SyncCryptoKeyInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The data currently stored on the Sync server is damaged.
        ///You can replace the data stored on the server by running &apos;0install sync --reset=server&apos;..
        /// </summary>
        internal static string SyncServerDataDamaged {
            get {
                return ResourceManager.GetString("SyncServerDataDamaged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find {0} with the ID &apos;{1}&apos;..
        /// </summary>
        internal static string UnableToFindTypeID {
            get {
                return ResourceManager.GetString("UnableToFindTypeID", resourceCulture);
            }
        }
    }
}
