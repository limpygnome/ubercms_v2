/*                       ____               ____________
 *                      |    |             |            |
 *                      |    |             |    ________|
 *                      |    |             |   |
 *                      |    |             |   |    
 *                      |    |             |   |    ____
 *                      |    |             |   |   |    |
 *                      |    |_______      |   |___|    |
 *                      |            |  _  |            |
 *                      |____________| |_| |____________|
 *                        
 *      Author(s):      limpygnome (Marcus Craske)              limpygnome@gmail.com
 * 
 *      License:        Creative Commons Attribution-ShareAlike 3.0 Unported
 *                      http://creativecommons.org/licenses/by-sa/3.0/
 * 
 *      File:           App_code/CMS/Base/Settings.cs
 *      Path:           /Settings.cs
 * 
 *      Change-Log:
 *                      2013-06-27      Moved this class, as a sub-class, from Settings to its own class.
 *                                      Finished initial class.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Added changes for pluginid to UUID and more comments.
 * 
 * *********************************************************************************************************************
 * Represents a setting stored in a settings (CMS.Base.Settings) collection.
 * *********************************************************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Web;

namespace CMS.Base
{
    /// <summary>
    /// Represents a setting stored in a settings (CMS.Base.Settings) collection.
    /// </summary>
    public class SettingsNode
    {
        // Enums ***************************************************************************************************
        public enum SettingsNodeState
        {
            None,
            ModifiedValue,
            ModifiedDescription,
            ModifiedAll,
            Added
        }
        // Fields **************************************************************************************************
        private UUID uuid;                      // The owner (plugin) of the setting.
        private string value, description;      // The value and description of the setting.
        private SettingsNodeState state;        // The persisted state of the setting between this and the data-store.
        // Methods - Constructors **********************************************************************************
        /// <summary>
        /// Creates a new unmodified settings node with only a value.
        /// </summary>
        /// <param name="value"></param>
        public SettingsNode(string value)
        {
            this.value = value;
            this.state = SettingsNodeState.None;
        }
        /// <summary>
        /// Creates a new node with a value and state but no owner.
        /// </summary>
        /// <param name="value">The value of the node.</param>
        /// <param name="state">The state of the node.</param>
        public SettingsNode(string value, SettingsNodeState state)
        {
            this.state = state;
        }
        /// <summary>
        /// Creates a new node with an owner, value and description.
        /// </summary>
        /// <param name="value">The value of the node.</param>
        /// <param name="description">A description of the node.</param>
        /// <param name="uuid">The owner of the node.</param>
        public SettingsNode(string value, string description, UUID uuid)
        {
            this.value = value;
            this.description = description;
            this.uuid = uuid;
        }
        /// <summary>
        /// Creates a new node with an owner, description, value and state.
        /// </summary>
        /// <param name="value">The value of the node.</param>
        /// <param name="description">A description of the node.</param>
        /// <param name="uuid">The owner of the node.</param>
        /// <param name="state">The state of the node.</param>
        public SettingsNode(string value, string description, UUID uuid, SettingsNodeState state)
        {
            this.value = value;
            this.description = description;
            this.uuid = uuid;
            this.state = state;
        }
        // Methods - Properties ************************************************************************************
        /// <summary>
        /// The persisted state of this setting between this application and the data-store.
        /// </summary>
        public SettingsNodeState State
        {
            get
            {
                return state;
            }
        }
        /// <summary>
        /// The identifier of the plugin which owns this setting.
        /// </summary>
        public UUID OwnerUUID
        {
            get
            {
                return uuid;
            }
        }
        /// <summary>
        /// The value of this setting.
        /// </summary>
        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                state = state == SettingsNodeState.ModifiedDescription ? SettingsNodeState.ModifiedAll : SettingsNodeState.ModifiedValue;
            }
        }
        /// <summary>
        /// The description of this setting.
        /// </summary>
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
                state = state == SettingsNodeState.ModifiedValue ? SettingsNodeState.ModifiedAll : SettingsNodeState.ModifiedDescription;
            }
        }
    }
}