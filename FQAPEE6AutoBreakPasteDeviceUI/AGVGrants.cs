using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data.OleDb;

namespace AGV_WCF_Client.Model
{
    public struct Person
    {
        public string Name;
        public string Password;
        public int Level;
    }
    public struct Grant
    {
        public string Name;
        public string Scene;
        public string Model;
        public int Limits;
    }
    public class AGVGrants
    {
        public AGVGrants()
        {

        }
        public event EventHandler PersonsChanged;
        void OnPersonsChanged()
        {
            if (PersonsChanged != null)
                PersonsChanged(null, null);
        }
        public event EventHandler GrantsChanged;
        void OnGrantsChanged()
        {
            if (GrantsChanged != null)
                GrantsChanged(null, null);
        }

        #region 数据库连接器
        //Access
        public string StrAccess = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + System.IO.Directory.GetCurrentDirectory() + @"\Configure.accdb" + ";Persist Security Info=False";
        OleDbConnection OleDbConnect;

        //MySQL
        public string StrMySQL = "Server=192.168.2.18;Port=3306;Database=agv;Uid=ldr;Pwd=ldr123;Charset=utf8";
        MySqlConnection MySqlConnect;// = new MySqlConnection(StrMySQL);

        #endregion

        #region Person表

        public int CheckPerson(string Name,string Password)
        {
            OleDbConnect = new OleDbConnection(StrAccess);
            OleDbCommand cmd = OleDbConnect.CreateCommand();
            cmd.CommandText = "select password from person where name='" + Name + "'";

            OleDbConnect.Open();

            OleDbDataReader DBReader = cmd.ExecuteReader();
            string mPass = "";
            if (DBReader.HasRows)
            {
                while (DBReader.Read())
                {
                    mPass = DBReader.GetString(0);
                    Console.WriteLine("Password:" + mPass);
                }
                DBReader.Close();
            }
            else
            {
                DBReader.Close();
                return 2;//用户名不存在
            }
            if (mPass == Password)
                return 1;
            return 0;
        }

        //List<Person> Persons = new List<Person>();
        public void UpdatePersonTableFromServer()
        {
            AGVManager.Persons.Clear();
            MySqlConnect = new MySqlConnection(StrMySQL);
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

            MySqlCmd.CommandText = "select * from person";
            MySqlDataReader sdr = MySqlCmd.ExecuteReader();
            if (sdr.HasRows)
            {
                while (sdr.Read())
                {
                    Person p = new Person();
                    p.Name = sdr.GetString("name");
                    p.Password = sdr.GetString("password");
                    p.Level = sdr.GetInt32("level");
                    AGVManager.Persons.Add(p);
                }
            }
            MySqlConnect.Close();
            if (AGVManager.Persons.Count > 0)
            {
                OleDbConnect = new OleDbConnection(StrAccess);
                OleDbCommand OleDbCmd = OleDbConnect.CreateCommand();
                OleDbCmd.CommandText = "delete * from person";

                OleDbConnect.Open();

                OleDbCmd.ExecuteNonQuery();
                string SQLPreStr="insert into person ([name],[password],[level])";
                foreach (Person Current in AGVManager.Persons)
                {
                    string SQLStr = SQLPreStr + " values ('" +
                        Current.Name + "','" +
                        Current.Password + "'," +
                        Current.Level + ")";
                    OleDbCmd.CommandText = SQLStr;
                    OleDbCmd.ExecuteNonQuery();
                }

                OleDbConnect.Close();

                OnPersonsChanged();
            }
        }
        public void PersonTableRead()
        {

            OleDbConnect = new OleDbConnection(StrAccess);
            OleDbCommand cmd = OleDbConnect.CreateCommand();
            cmd.CommandText = "select count(*) from person";

            OleDbConnect.Open();

            OleDbDataReader DBReader = cmd.ExecuteReader();
            int Row;
            if (DBReader.HasRows)
            {
                while (DBReader.Read())
                {
                    Row = DBReader.GetInt32(0);
                    Console.WriteLine("Row:" + Row);
                }
            }
            DBReader.Close();

            AGVManager.Persons.Clear();

            cmd.CommandText = "select * from person";
            DBReader = cmd.ExecuteReader();
            if(DBReader.HasRows)
            {
                while(DBReader.Read())
                {
                    Person p = new Person();
                    DBReader.GetInt32(0);
                    p.Name = DBReader.GetString(1);
                    p.Password = DBReader.GetString(2);
                    p.Level = DBReader.GetInt32(3);
                    AGVManager.Persons.Add(p);
                    Console.WriteLine(p.Name + " " + p.Password + " " + p.Level);
                }
            }
            
            OleDbConnect.Close();
        }

        #endregion

        #region Grants表

        List<Grant> Grants = new List<Grant>();
        public void UpdateGrantsTableFromServer()
        {
            Grants.Clear();
            MySqlConnect = new MySqlConnection(StrMySQL);
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

            MySqlCmd.CommandText = "select * from grants where scene='"+"AGV1"+"' or scene='All'";
            MySqlDataReader sdr = MySqlCmd.ExecuteReader();
            if (sdr.HasRows)
            {
                while (sdr.Read())
                {
                    Grant g = new Grant();
                    g.Name = sdr.GetString("name");
                    g.Scene = sdr.GetString("scene");
                    g.Model = sdr.GetString("model");
                    g.Limits = sdr.GetInt32("limits");
                    Grants.Add(g);
                }
            }
            MySqlConnect.Close();
            if (Grants.Count > 0)
            {
                OleDbConnect = new OleDbConnection(StrAccess);
                OleDbCommand OleDbCmd = OleDbConnect.CreateCommand();
                OleDbCmd.CommandText = "delete * from grants";

                OleDbConnect.Open();

                OleDbCmd.ExecuteNonQuery();
                string SQLPreStr = "insert into grants ([name],[scene],[model],[limits])";
                foreach (Grant Current in Grants)
                {
                    string SQLStr = SQLPreStr + " values ('" +
                        Current.Name + "','" +
                        Current.Scene + "','" +
                        Current.Model + "'," +
                        Current.Limits + ")";
                    OleDbCmd.CommandText = SQLStr;
                    OleDbCmd.ExecuteNonQuery();
                }

                OleDbConnect.Close();
            }
        }
        public void GrantsTableRead()
        {

            OleDbConnect = new OleDbConnection(StrAccess);
            OleDbCommand cmd = OleDbConnect.CreateCommand();
            cmd.CommandText = "select count(*) from grants";

            OleDbConnect.Open();

            OleDbDataReader DBReader = cmd.ExecuteReader();
            int Row;
            if (DBReader.HasRows)
            {
                while (DBReader.Read())
                {
                    Row = DBReader.GetInt32(0);
                    Console.WriteLine("Row:" + Row);
                }
            }
            DBReader.Close();

            Grants.Clear();

            cmd.CommandText = "select * from grants";
            DBReader = cmd.ExecuteReader();
            if (DBReader.HasRows)
            {
                while (DBReader.Read())
                {
                    Grant g = new Grant();
                    DBReader.GetInt32(0);//ID
                    g.Name = DBReader.GetString(1);
                    g.Scene = DBReader.GetString(2);
                    g.Model = DBReader.GetString(3);
                    g.Limits = DBReader.GetInt32(4);
                    Grants.Add(g);
                    Console.WriteLine(g.Name + " " + g.Scene + " " + g.Model + " " + g.Limits);
                }
            }

            OleDbConnect.Close();
        }
        public int CheckLimits(string Name,string Model)
        {
            OleDbConnect = new OleDbConnection(StrAccess);
            OleDbCommand cmd = OleDbConnect.CreateCommand();
            cmd.CommandText = "select limits from grants where name='" + Name + "' and (model='" + Model + "' or model='All')";

            OleDbConnect.Open();

            OleDbDataReader DBReader = cmd.ExecuteReader();
            int limits = 0; ;
            if (DBReader.HasRows)
            {
                while (DBReader.Read())
                {
                    //DBReader.GetInt32(0);//ID
                    limits = DBReader.GetInt32(0);
                    Console.WriteLine("Limits:" + limits);
                }
            }
            DBReader.Close();

            OleDbConnect.Close();
            return limits;
        }

        #endregion
    }
}
