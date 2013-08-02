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
 *      Path:           /App_Code/CMS/Base/Settings.cs
 * 
 *      Change-Log:
 *                      2013-06-27      Moved this class, as a sub-class, from Settings to its own class.
 *                                      Finished initial class.
 *                      2013-07-21      Code format changes and UberLib.Connector upgrade.
 *                                      Added changes for pluginid to UUID and more comments.
 *                      2013-07-25      Moved parsing of values to a public static method, for usage elsewhere.
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
        // Enums *******************************************************************************************************
        /// <summary>
        /// The state of the node.
        /// </summary>
        public enum SettingsNodeState
        {
            None,
            ModifiedValue,
            ModifiedDescription,
            ModifiedAll,
            Added
        }
        /// <summary>
        /// The type of data of the node.
        /// </summary>
        public enum DataType
        {
            String = 0,
            Integer = 1,
            Float = 2,
            Double = 3,
            Bool = 4,
            Null = 16
        }
        // Fields ******************************************************************************************************
        private UUID uuid;                      // The owner (plugin) of the setting.
        private string description;             // The description of the setting.
        private SettingsNodeState state;        // The persisted state of the setting between this and the data-store.
        private object value;                   // The value of the node.
        private DataType type;                  // The data-type of the node.
        // Methods - Constructors **************************************************************************************
        /// <summary>
        /// Creates a new unmodified settings node with only a value.
        /// </summary>
        /// <param name="type">The type of value specified.</param>
        /// <param name="value">The string value of the node</param>
        public SettingsNode(DataType type, string value)
        {
            this.type = type;
            this.value = parseTypeValue(this.type, value);
            this.state = SettingsNodeState.None;
        }
        /// <summary>
        /// Creates a new node with a value and state but no owner.
        /// </summary>
        /// <param name="type">The type of value specified.</param>
        /// <param name="value">The string value of the node.</param>
        /// <param name="state">The state of the node.</param>
        public SettingsNode(DataType type, string value, SettingsNodeState state)
        {
            this.type = type;
            this.value = parseTypeValue(this.type, value);
            this.state = state;
        }
        /// <summary>
        /// Creates a new node with an owner, value and description.
        /// </summary>
        /// <param name="type">The type of value specified.</param>
        /// <param name="value">The string value of the node.</param>
        /// <param name="description">A description of the node.</param>
        /// <param name="uuid">The owner of the node.</param>
        public SettingsNode(DataType type, string value, string description, UUID uuid)
        {
            this.type = type;
            this.value = parseTypeValue(this.type, value);
            this.description = description;
            this.uuid = uuid;
            this.state = SettingsNodeState.None;
        }
        /// <summary>
        /// Creates a new node with an owner, description, value and state.
        /// </summary>
        /// <param name="type">The type of value specified.</param>
        /// <param name="value">The string value of the node.</param>
        /// <param name="description">A description of the node.</param>
        /// <param name="uuid">The owner of the node.</param>
        /// <param name="state">The state of the node.</param>
        public SettingsNode(DataType type, string value, string description, UUID uuid, SettingsNodeState state)
        {
            this.type = type;
            this.value = parseTypeValue(this.type, value);
            this.description = description;
            this.uuid = uuid;
            this.state = state;
        }
        // Methods *****************************************************************************************************
        /// <summary>
        /// Parses the specified string value into the node's internal data-type and sets the value as that type.
        /// </summary>
        /// <param name="value">The new value for the node.</param>
        public void setValue(string value)
        {
            this.value = parseTypeValue(this.type, value);
            state = state == SettingsNodeState.ModifiedDescription ? SettingsNodeState.ModifiedAll : SettingsNodeState.ModifiedValue;
        }
        /// <summary>
        /// Parses a setting's value into the specified data-type.
        /// </summary>
        /// <param name="type">The data-type the value is to be parsed into.</param>
        /// <param name="value">The value to be parsed.</param>
        /// <returns>The parsed value.</returns>
        public static object parseTypeValue(DataType type, string value)
        {
            switch (type)
            {
                case DataType.String:
                    return value;
                case DataType.Integer:
                    return int.Parse(value);
                case DataType.Float:
                    return float.Parse(value);
                case DataType.Double:
                    return double.Parse(value);
                case DataType.Bool:
                    return value == "1" || value.ToLower() == "true";
                case DataType.Null:
                    return null;
                default:
                    throw new InvalidOperationException("Unknown data-type specified for value '" + value + "'; type: '" + (int)type + "'!");
            }
        }
        // Methods - Static ********************************************************************************************
        /// <summary>
        /// Parses the string type into a data-type enum.
        /// </summary>
        /// <param name="type">String numeric value of the data-type.</param>
        /// <returns>The parsed data-type as an enum.</returns>
        public static DataType parseType(string type)
        {
            return (DataType)Enum.Parse(typeof(DataType), type);
        }
        /// <summary>
        /// Parses an integer type into a data-type-enum.
        /// </summary>
        /// <param name="type">The numeric value of the data-type.</param>
        /// <returns>The parsed data-type as an enum.</returns>
        public static DataType parseType(int type)
        {
            return (DataType)type;
        }
        // Methods - Accessors *****************************************************************************************
        /// <summary>
        /// Fetches the value as a type; there is no type safety, use with caution!
        /// </summary>
        /// <typeparam name="T">The type of the object; refer to DataType enum in this class for supported types.</typeparam>
        /// <returns>The object as the specified type.</returns>
        public T get<T>()
        {
            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException("Invalid cast of data-type '" + typeof(T).ToString() + "' from '" + value.GetType().ToString() + "' (node-tpye: '" + type.ToString() + "') for node '" + description + "'!");
            }
        }
        public override string ToString()
        {
            return value.ToString();
        }
        // Methods - Properties ****************************************************************************************
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
        /// 
        /// Warning: when setting this property, the specified value is not parsed into the internal type!
        /// </summary>
        public object Value
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
        /// <summary>
        /// The data-type of the value.
        /// </summary>
        public DataType ValueDataType
        {
            get
            {
                return type;
            }
            set
            {
                this.type = value;
                state = state == SettingsNodeState.ModifiedDescription ? SettingsNodeState.ModifiedAll : SettingsNodeState.ModifiedValue;
            }
        }
    }
}