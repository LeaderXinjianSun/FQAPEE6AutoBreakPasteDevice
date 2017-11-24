using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace FQAPEE6AutoBreakPasteDeviceUI
{
    public class MySQLClass
    {
        public string StrMySQL = "Server=10.86.16.109;Database=dnc;Uid=dnc;Pwd=qhddnc*168";
        
        public void test()
        {
            MySqlConnection MySqlConnect = new MySqlConnection(StrMySQL);
            MySqlCommand MySqlCmd = MySqlConnect.CreateCommand();
            try
            {
                MySqlConnect.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            MySqlCmd.CommandText = "SELECT * FROM dnc.SPI_INI WHERE  BARCODE_ID = 'N70-WQ5-H85118-0109'";
            MySqlDataReader sdr = MySqlCmd.ExecuteReader();
            if (sdr.HasRows)
            {
                while (sdr.Read())
                {
                    //Grant g = new Grant();
                    Console.WriteLine(sdr.GetValue(0));
                    //g.Scene = sdr.GetString("scene");
                    //g.Model = sdr.GetString("model");
                    //g.Limits = sdr.GetInt32("limits");
                    //Grants.Add(g);
                }
            }
            MySqlConnect.Close();
        }

    }
}
