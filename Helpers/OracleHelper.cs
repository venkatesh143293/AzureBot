using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.Helpers
{
    public class OracleHelper
    {
        public static List<string> getOracleDBBranches()
        {
            try
            {
                List<string> lstDbrepos = new List<string>();
                string connString = "Data Source=VPMTST1.ihtech.com:1521/VPMTST1.iht.com;User Id=USER_MASTER_SELECT;Password=USER_MASTER_SELECT;";
                string data = string.Empty;
                using (OracleConnection connection = new OracleConnection(connString))
                {
                    using (OracleCommand command = connection.CreateCommand())
                    {
                        connection.Open();
                        command.BindByName = true;
                        command.CommandText = "WITH CTE AS (SELECT SUBSTR(RULES.UTILS.GET_APP_VERSION,INSTR(RULES.UTILS.GET_APP_VERSION, '-') + 1,INSTR(RULES.UTILS.GET_APP_VERSION, '-', -1)- INSTR(RULES.UTILS.GET_APP_VERSION, '-')" +
                            " - 1) + SNO AS RELEASE FROM DUAL CROSS JOIN(SELECT 0 SNO FROM DUAL UNION SELECT 1 SNO FROM DUAL UNION SELECT 2 AS SNO FROM DUAL )" +
                            " SNO) SELECT 'ICM-DEV-9-' || RELEASE || '-0'   FROM CTE  WHERE INSTR(RULES.UTILS.GET_APP_VERSION @PMPRD1, RELEASE)= 0";
                        OracleDataReader reader = command.ExecuteReader();
                        int i = 0;
                        while (reader.Read())
                        {
                            lstDbrepos.Add(reader.GetString(i));
                        }
                        reader.Dispose();
                    }
                }
                return lstDbrepos;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
    }
}
