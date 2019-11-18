using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Configuration;
using System.Web.Script.Services;
using System.Timers;
using System.Web.UI;

namespace WS_QLTS
{
    /// <summary>
    /// Summary description for QLTS_ORG
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class QLTS_ORG : System.Web.Services.WebService
    {
        static string connectionString = ConfigurationManager.ConnectionStrings["QLTS_LGE"].ConnectionString;
        static string connect_to_QLTS = ConfigurationManager.ConnectionStrings["QLTS_DB"].ConnectionString;
        OracleConnection con = new OracleConnection(connectionString);
        OracleConnection connection = new OracleConnection(connect_to_QLTS);
        OracleConnection updateconnection = new OracleConnection(connect_to_QLTS);
        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }
        [WebMethod]
        public DataTable HR_ORG()
        {
            string SQL_ORG = "select distinct ORGANIZATION_ID, ORG_NAME_ENG from XVH_EMP_MASTER_IF";
            OracleDataAdapter daORG = new OracleDataAdapter(SQL_ORG, con);
            DataTable dtORG = new DataTable("LGE_ORG");
            DataSet dsORG = new DataSet();
            dsORG.Tables.Add(dtORG);
            daORG.Fill(dtORG);

            return dtORG;
        }
        [WebMethod]
        public DataTable HR_INFOR(string MaNV)
        {
            string strHuman = "select a.*, b.MOBILE_NUMBER from VHPRL.EMP_MASTER a " +
                "inner join XVH_EMP_INFO_IF b on a.EMPLOYEE_NUMBER = b.EMPLOYEE_NUMBER " +
                "where a.employee_number = '" + MaNV + "' and b.mobile_number is not null";

            string strHuman2 = "select a.*, b.MOBILE_NUMBER from VHPRL.EMP_MASTER a " +
                "inner join XVH_EMP_INFO_IF b on a.EMPLOYEE_NUMBER = b.EMPLOYEE_NUMBER " +
                "where a.employee_number = '" + MaNV + "'";

            OracleDataAdapter daHuman = new OracleDataAdapter(strHuman, con);
            DataTable dtHR = new DataTable("HR_Info");
            DataTable dtResult = new DataTable("HR_Result");
            daHuman.Fill(dtHR);
            if (dtHR.Rows.Count == 0)
            {
                daHuman = new OracleDataAdapter(strHuman2, con);
                daHuman.Fill(dtResult);

            }
            else if (dtHR.Rows.Count != 0)
            {
                dtResult = dtHR;
            }

            return dtResult;
        }

        [WebMethod]
        public void CheckResignation()
        {
            CheckNgoaiKho();
            KiemTraNhanSuDaBanGiaoVatTu();

            string QLTS_NS = "select ID from tb_user where Emp_Status = 'EMP'";
            OracleDataAdapter daqltsns = new OracleDataAdapter(QLTS_NS, connection);
            DataTable dtqltsns = new DataTable();
            daqltsns.Fill(dtqltsns);

            foreach (DataRow row in dtqltsns.Rows)
            {
                var MNV = row["ID"].ToString();
                string LGE_STATUS = "select EMPLOYEE_NUMBER, STATUS from VHPRL.EMP_MASTER where EMPLOYEE_NUMBER = '" + MNV + "'";

                OracleDataAdapter daLGE = new OracleDataAdapter(LGE_STATUS, con);
                DataTable dtLGE = new DataTable();
                daLGE.Fill(dtLGE);
                if (dtLGE.Rows[0]["STATUS"].ToString() == "Resignation")
                {
                    string updateQLTS_HR = "update TB_USER set EMP_STATUS = 'REG' where ID = '" + MNV + "'";
                    OracleCommand cmdUpdateQLTS = new OracleCommand(updateQLTS_HR, updateconnection);
                    updateconnection.Open();
                    cmdUpdateQLTS.ExecuteNonQuery();
                    updateconnection.Close();


                    string YCTHTS = "insert into NSCBGTS (MANV, REMARK, BAN_GIAO, NGAY_UPDATE) values (:MaNV, :remark, :bg, CURRENT_DATE)";
                    OracleCommand cmdToNSCBGTS = new OracleCommand(YCTHTS, updateconnection);
                    cmdToNSCBGTS.Parameters.Add(new OracleParameter("MaNV", MNV));
                    cmdToNSCBGTS.Parameters.Add(new OracleParameter("remark", "nhân sự đã nghỉ việc"));
                    cmdToNSCBGTS.Parameters.Add(new OracleParameter("bg", "Y"));
                    updateconnection.Open();
                    cmdToNSCBGTS.ExecuteNonQuery();
                    updateconnection.Close();
                }
                else if (dtLGE.Rows[0]["STATUS"].ToString() == "Employment")
                {

                }


            }
            string CheckBGTS = "select * from NSCBGTS";
            OracleDataAdapter daBGTS = new OracleDataAdapter(CheckBGTS, connection);
            DataTable dtBGTS = new DataTable();
            daBGTS.Fill(dtBGTS);
            foreach (DataRow dataRow in dtBGTS.Rows)
            {
                var regNV = dataRow["MANV"].ToString();
                string CheckTSNGKH = "select a.Ma_TS, a.So_BB from ngoai_kho a " +
                    "inner join BIEN_BAN b on a.So_BB = b.So_Bien_Ban " +
                    " where b.USER_ID = '" + regNV + "'";
                OracleDataAdapter daTSNGKH = new OracleDataAdapter(CheckTSNGKH, connection);
                DataTable dtTSNGKH = new DataTable();
                daTSNGKH.Fill(dtTSNGKH);
                if (dtTSNGKH.Rows.Count > 0)
                {
                    string updateNSCBGTS = "update NSCBGTS set BAN_GiAO = 'N', REMARK = 'nhân sự chưa bàn giao tài sản' where MANV = '" + regNV + "'";
                    OracleCommand cmdupdateNSCHBGTS = new OracleCommand(updateNSCBGTS, updateconnection);
                    updateconnection.Open();
                    cmdupdateNSCHBGTS.ExecuteNonQuery();
                    updateconnection.Close();
                }
                else if (dtTSNGKH.Rows.Count == 0)
                {

                }
            }

        }

        [WebMethod]
        public void KiemTraNhanSuDaBanGiaoVatTu()
        {
            //CheckNgoaiKho();
            string CheckBGTS2 = "select * from NSCBGTS where BAN_GIAO = 'N'";
            OracleDataAdapter daBGTS2 = new OracleDataAdapter(CheckBGTS2, connection);
            DataTable dtBGTS2 = new DataTable();
            daBGTS2.Fill(dtBGTS2);
            foreach (DataRow row in dtBGTS2.Rows)
            {
                var regNV = row["MANV"].ToString();
                string CheckTSNGKH = "select a.Ma_TS, a.So_BB from ngoai_kho a " +
                    "inner join BIEN_BAN b on a.So_BB = b.So_Bien_Ban where b.USER_ID = '" + regNV + "'";
                OracleDataAdapter daTSNGKH = new OracleDataAdapter(CheckTSNGKH, connection);
                DataTable dtTSNGKH = new DataTable();
                daTSNGKH.Fill(dtTSNGKH);
                if (dtTSNGKH.Rows.Count == 0)
                {
                    //string updateNSCBGTS = "update NSCBGTS set BAN_GiAO = 'Y', NGAY_UPDATE = CURRENT_DATE, REMARK = 'nhân sự đã bàn giao tài sản' where MANV = '" + regNV + "'";
                    string updateNSCBGTS = "delete from NSCBGTS where MANV = '" + regNV + "'";
                    OracleCommand cmdupdateNSCHBGTS = new OracleCommand(updateNSCBGTS, updateconnection);
                    updateconnection.Open();
                    cmdupdateNSCHBGTS.ExecuteNonQuery();
                    updateconnection.Close();
                }
            }

        }
        public void CheckNgoaiKho()
        {
            string checkOutStorage = "select a.So_BB, b.USER_ID, c.EMP_STATUS from Ngoai_kho a " +
                "inner join Bien_Ban b on a.So_BB = b.So_Bien_Ban " +
                "inner join TB_USER c on c.ID = b.USER_ID " +
                "where c.EMP_STATUS = 'REG'";
            OracleDataAdapter daRecheckNgoaiKho = new OracleDataAdapter(checkOutStorage, updateconnection);
            DataTable dtRecheckNgoaiKho = new DataTable();
            daRecheckNgoaiKho.Fill(dtRecheckNgoaiKho);
            if (dtRecheckNgoaiKho.Rows.Count != 0)
            {
                foreach (DataRow row in dtRecheckNgoaiKho.Rows)
                {
                    string updateNSCBGTS = "insert into NSCBGTS (MANV, REMARK, BAN_GIAO, NGAY_UPDATE) values (:id, :remark, :bg, CURRENT_DATE)";
                    OracleCommand cmdUpdate = new OracleCommand(updateNSCBGTS, updateconnection);
                    cmdUpdate.Parameters.Add(new OracleParameter("id", row["USER_ID"].ToString()));
                    cmdUpdate.Parameters.Add(new OracleParameter("remark", "bộ phận quản lý tài sản chưa thu hồi"));
                    cmdUpdate.Parameters.Add(new OracleParameter("bg", "N"));
                    updateconnection.Open();
                    cmdUpdate.ExecuteNonQuery();
                    updateconnection.Close();
                }
            }
        }
    }
}
