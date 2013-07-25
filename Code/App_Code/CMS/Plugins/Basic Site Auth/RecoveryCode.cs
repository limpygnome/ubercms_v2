
using System;
using CMS.Base;
using UberLib.Connector;

namespace CMS.BasicSiteAuth
{
    public class RecoveryCode
    {
        // Fields ******************************************************************************************************
        private bool        persisted,
                            modified;
        private string      oldCode,
                            code;
        private int         userID;
        private DateTime    datetimeCreated;
        // Methods - Constructors **************************************************************************************
        public RecoveryCode()
        {
            this.modified = this.persisted = false;
        }
        // Methods *****************************************************************************************************
        public void generateNewCode()
        {
            this.code = BaseUtils.generateRandomString(32);
        }
        // Methods - Database Persistence ******************************************************************************
        public RecoveryCode load(int userID, string code, Connector conn)
        {
            Result data = conn.queryRead("SELECT * FROM bsa_recovery_codes WHERE code='" + SQLUtils.escape(code) + "' AND userid='" + SQLUtils.escape(userID.ToString()) + "'");
            if (data.Count == 1)
                return load(data[0]);
            else
                return null;
        }
        public RecoveryCode load(ResultRow row)
        {
            RecoveryCode code = new RecoveryCode();
            code.code = code.oldCode = row.get2<string>("code");
            code.userID = row.get2<int>("userid");
            code.datetimeCreated = row.get2<DateTime>("datetime_created");
            return code;
        }
        public void save(Connector conn)
        {
            if (!modified)
                return;
            SQLCompiler c = new SQLCompiler();
            c["code"] = code;
            c["userid"] = userID.ToString();
            c["datetime_created"] = datetimeCreated.ToString("YYYY-MM-dd HH:mm:ss");
            if(persisted)
                conn.queryExecute(c.compileUpdate("bsa_recovery_codes", "code='" + SQLUtils.escape(oldCode) + "'");
            else
            {
                int attempts = 0;
                while(attempts < 5)
                {
                    try
                    {
                        conn.queryExecute(c.compileInsert("bsa_recovery_codes"));
                        break;
                    }
                    catch(DuplicateEntryException ex)
                    {
                        attempts++;
                        // Generate new recovery code
                        generateNewCode();
                        c["code"] = code;
                    }
                }
            }
            oldCode = code;
            modified = false;
        }
        public void remove()
        {

            persisted = false;
            modified = true;
        }
        // Methods - Properties ****************************************************************************************
        public string Code
        {
            get
            {
                return code;
            }
            set
            {
                code = value;
                modified = true;
            }
        }

        public int UserID
        {
            get
            {
                return userid;
            }
            set
            {
                userid = value;
                modified = true;
            }
        }

        public DateTime DateTimeCreated
        {
            get
            {
            }
            set
            {
            }
        }
    }
}