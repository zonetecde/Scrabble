﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Scrabble.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.2.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0,0;0,7;0,14;7,0;14,0;14,7;14,14;7,14")]
        public string redCell {
            get {
                return ((string)(this["redCell"]));
            }
            set {
                this["redCell"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1,1;2,2;3,3;4,4;13,1;12,2;11,3;10,4;1,13;2,12;3,11;4,10;13,13;12,12;11,11;10,10")]
        public string pinkCell {
            get {
                return ((string)(this["pinkCell"]));
            }
            set {
                this["pinkCell"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0,3;0,11;3,0;2,6;2,8;3,7;3,14;6,2;6,6;6,8;6,12;7,3;7,11;8,2;8,6;8,8;8,12;11,0;11," +
            "7;11,14;12,6;12,8;14,3;14,11")]
        public string lightBlueCell {
            get {
                return ((string)(this["lightBlueCell"]));
            }
            set {
                this["lightBlueCell"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1,5;1,9;5,1;5,5;5,9;5,13;9,1;9,5;9,9;9,13;13,5;13,9")]
        public string blueCell {
            get {
                return ((string)(this["blueCell"]));
            }
            set {
                this["blueCell"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(" ,0;EAINORSTUL,1;DGM,2;BCP,3;FHV,4;JQ,8;KWXYZ,10")]
        public string letterScore {
            get {
                return ((string)(this["letterScore"]));
            }
            set {
                this["letterScore"] = value;
            }
        }
    }
}
