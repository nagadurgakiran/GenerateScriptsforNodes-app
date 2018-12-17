using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.OleDb;
using System.IO;
using System.Data.SqlClient;
using System.Data.Common;

namespace GenerateScriptsforNodes
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ReadExcelData();
        }

        private void ReadExcelData()
        {
            string path = @"E:\testingexcell.xls";
            IList<NodeDetails> objExcelCon = ReadExcel(path);

            var nodeCodeList = objExcelCon.Select(x => x.Restriction).Distinct().ToList();

            //ExecuteNodeCodes(nodeCodeList);

            var oldNodeCode = "";
            var newNodeCode = "";
            var originatorInboxNodeID = 0;
            var originatorInboxUserNodeID = 0;
            var nodeCodeApproverNodeID = 0;

            var maskFileScript = "";
            var userGroupScript = "";
            var userAccessScript = "";

            for (int i = 0; i < objExcelCon.Count(); i++)
            {                
                newNodeCode = objExcelCon[i].Restriction.ToString();
                if (newNodeCode != "")
                {

                    if (oldNodeCode == "" || (oldNodeCode != newNodeCode))
                    {
                        var NodeCodeDetails = GetNodeCodeDetails(newNodeCode);
                        originatorInboxNodeID = Convert.ToInt32(NodeCodeDetails.Tables[0].Rows[0][0].ToString());
                        originatorInboxUserNodeID = Convert.ToInt32(NodeCodeDetails.Tables[0].Rows[1][0].ToString());
                        nodeCodeApproverNodeID = Convert.ToInt32(NodeCodeDetails.Tables[0].Rows[2][0].ToString());

                        oldNodeCode = newNodeCode;

                        maskFileScript += "--" + newNodeCode + "  -- " + nodeCodeApproverNodeID + " \n";
                        maskFileScript += "-----------------------------------  \n";
                        userGroupScript += "--" + newNodeCode + " \n";
                        userGroupScript += "----------------------------------  \n";
                        userAccessScript += "--" + newNodeCode + " \n";
                        userAccessScript += "----------------------------------  \n";
                    }
                    if (Convert.ToInt32(objExcelCon[i].AmountLimit) != 0)
                        maskFileScript += "insert into MaskFile_Hierarchy values('" + objExcelCon[i].UserDetailsID.ToString() + "','" + objExcelCon[i].Restriction.ToString() + "'," + Convert.ToInt32(objExcelCon[i].AmountLimit) + "," + nodeCodeApproverNodeID + ",22); \n";

                    if (Convert.ToInt32(objExcelCon[i].AmountLimit) == 0)
                        userGroupScript += "insert into workflow_group values(" + originatorInboxUserNodeID + ",'" + objExcelCon[i].UserDetailsID.ToString() + "',0,''); \n";
                    else
                        userGroupScript += "insert into workflow_group values(" + nodeCodeApproverNodeID + ",'" + objExcelCon[i].UserDetailsID.ToString() + "',0,''); \n";

                    userAccessScript += "IF NOT EXISTS(SELECT * FROM UserAccess WHERE KEYVALUE = '" + objExcelCon[i].Restriction.ToString() + "' AND USERDETAILSID = '" + objExcelCon[i].UserDetailsID.ToString() + "') \n";

                    userAccessScript += "BEGIN \n";

                    userAccessScript += "Insert Into UserAccess values('" + objExcelCon[i].UserDetailsID.ToString() + "', 231, 38, '" + objExcelCon[i].Restriction.ToString() + "', Convert(varbinary(500), '')) \n";

                    userAccessScript += "END \n";
                }
            }
            maskFileScript += "-----------------------------------  \n";
            userGroupScript += "----------------------------------  \n";
            userAccessScript += "----------------------------------  \n";

            //ExcecuteScript(maskFileScript);
            //ExcecuteScript(userGroupScript);
            //ExcecuteScript(userAccessScript);

        }

        private DataSet GetNodeCodeDetails(string newNodeCode)
        {
            DataSet ds = new DataSet();
            try
            {
                SqlConnection conn = new SqlConnection("Data Source=hydddiser;Initial Catalog=DocAgent_SWIFT;User ID=ddiarchive;Password=CBdcHq2@pm18mvEzr6F5Mmw==?");
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "select * from workflow_node where Description like '%" + newNodeCode + "%' order by WorkFlowNodeID ";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                //Console.WriteLine("Inserting Data Successfully");
                conn.Close();

            }
            catch (Exception e)
            {
                //Console.WriteLine("Exception Occre while creating SP:" + e.Message + "\t" + e.GetType());
            }
            return ds;
        }

        private void ExecuteNodeCodes(List<string> nodeCodeList)
        {
            if (nodeCodeList.Count() > 0)
            {
                var sqlScript = "";
                for (int i = 0; i < nodeCodeList.Count(); i++)
                {
                    sqlScript += "exec [InsertNodeCode_In_Transition_Conditions_new] '" + nodeCodeList[i] + "' \n";
                    sqlScript += "GO \n";
                }

                ExcecuteScript(sqlScript);

            }
        }

        private void ExcecuteScript(string sqlScript)
        {
            try
            {
                SqlConnection conn = new SqlConnection("Data source=USER-PC; Database=Emp123;User Id=sa;Password=sa123");
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = sqlScript;
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();
                Console.WriteLine("Inserting Data Successfully");
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Occre while creating SP:" + e.Message + "\t" + e.GetType());
            }
            Console.ReadKey();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var userList = txtUsers.Text.Trim().Split('\n');
            var amountList = txtAmounts.Text.Trim().Split('\n');
            var queryScript = "";

            if (userList.Count() > 0)
            {
                queryScript += "\n";
                queryScript += "-- " + txtNodeCode.Text + " \n";
                queryScript += "----------------------------------------------- \n";


                foreach (var item in userList)
                {
                    queryScript += "IF NOT EXISTS(SELECT * FROM UserAccess WHERE KEYVALUE = '" + txtNodeCode.Text + "' AND USERDETAILSID = '" + item + "') \n";

                    queryScript += "BEGIN \n";

                    queryScript += "Insert Into UserAccess values('" + item + "', 231, 38, '" + txtNodeCode.Text + "', Convert(varbinary(500), '')) \n";

                    queryScript += "END \n";
                }
            }

            queryScript += "----------------------------------------------- \n";
            queryScript += "\n";

            txtScript.Text = queryScript;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtNodeCode.Text = "";
            txtScript.Text = "";
            txtUsers.Text = "";
        }

        private OleDbConnection OpenConnection(string path)
        {
            OleDbConnection oledbConn = null;
            try
            {
                if (Path.GetExtension(path) == ".xls")
                    oledbConn = new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + path +

"; Extended Properties= \"Excel 8.0;HDR=Yes;IMEX=2\"");
                else if (Path.GetExtension(path) == ".xlsx")
                    oledbConn = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" +

path + "; Extended Properties='Excel 12.0;HDR=YES;IMEX=1;';");

                oledbConn.Open();
            }
            catch (Exception ex)
            {
                //Error
            }
            return oledbConn;
        }

        private IList<NodeDetails> ExtractNodeDetailsExcel(OleDbConnection oledbConn)
        {
            OleDbCommand cmd = new OleDbCommand(); ;
            OleDbDataAdapter oleda = new OleDbDataAdapter();
            DataSet dsNodeDetailsInfo = new DataSet();

            cmd.Connection = oledbConn;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT * FROM [Node WorkFlow$]"; //Excel Sheet Name ( Employee )
            oleda = new OleDbDataAdapter(cmd);
            oleda.Fill(dsNodeDetailsInfo, "NodeDetails");

            var dsEmployeeInfoList = dsNodeDetailsInfo.Tables[0].AsEnumerable().Select(s => new NodeDetails
            {
                UserDetailsID = Convert.ToString(s["UserDetailsID"] != DBNull.Value ? s["UserDetailsID"] : ""),
                FirstName = Convert.ToString(s["FirstName"] != DBNull.Value ? s["FirstName"] : ""),
                LastName = Convert.ToString(s["LastName"] != DBNull.Value ? s["LastName"] : ""),
                EmailID = Convert.ToString(s["EmailID"] != DBNull.Value ? s["EmailID"] : ""),
                Restriction = Convert.ToString(s["Restriction"] != DBNull.Value ? s["Restriction"] : ""),
                AmountLimit = Convert.ToString(s["AmountLimit"] != DBNull.Value ? s["AmountLimit"] : ""),
                Role = Convert.ToString(s["Role"] != DBNull.Value ? s["Role"] : ""),
                Notes = Convert.ToString(s["Notes"] != DBNull.Value ? s["Notes"] : "")
            }).ToList();

            return dsEmployeeInfoList;
        }

        public IList<NodeDetails> ReadExcel(string path)
        {
            IList<NodeDetails> objNodeDetailsInfo = new List<NodeDetails>();
            OleDbConnection oledbConn = OpenConnection(path);
            try
            {

                if (oledbConn.State == ConnectionState.Open)
                {
                    objNodeDetailsInfo = ExtractNodeDetailsExcel(oledbConn);
                    oledbConn.Close();
                }


            }
            catch (Exception ex)
            {
                // Error
            }

            return objNodeDetailsInfo;
        }

    }

    public class NodeDetails
    {
        public string UserDetailsID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailID { get; set; }
        public string Restriction { get; set; }
        public string AmountLimit { get; set; }
        public string Role { get; set; }
        public string Notes { get; set; }
    }
}