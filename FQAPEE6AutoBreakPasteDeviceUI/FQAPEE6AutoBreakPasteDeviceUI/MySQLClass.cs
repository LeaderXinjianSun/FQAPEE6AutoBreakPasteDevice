using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
<<<<<<< HEAD
using System.Data;

namespace FQAPEE6AutoBreakPasteDeviceUI
{
    public class MySQLClass 
    {
        public string StrMySQL = "Server=10.86.16.109;Database=dnc;Uid=dnc;Pwd=qhddnc*168";
        public string FindResult(string barcode)
        {
            MySqlConnection conn = null;
            MySqlDataReader rdr = null;
            string result = "";
            try
            {
                conn = new MySqlConnection(StrMySQL);
                conn.Open();

                string stm = "SELECT TOTAL_DEVICE_LAYOUT FROM dnc.SPI_INI WHERE  BARCODE_ID ='" + barcode + "'";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    result = rdr.GetString(0);
                }
                else
                {
                    result = "Empty";
                }
            }
            catch (MySqlException ex)
            {

                Console.WriteLine("Error: {0}", ex.ToString());
                result = "Error";
            }
            finally
            {
                if (rdr != null)
                {
                    rdr.Close();
                }
                if (conn != null)
                {
                    conn.Close();
                }
            }
            return result;
        }
        
=======

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
>>>>>>> 46aa52aabcac5f481d37b8e1f6666a5db81d13d6

    }
}
