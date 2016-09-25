#define DEBUG_Trn
#undef DEBUG_Trn
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClassXMLFileIO;
using Ledger.ClassCom;
using System.Transactions;
using FarPoint.Win.Spread;
using GrapeCity.Win.Editors;
using ClassLibrary1;
using ClassDao;
namespace BreedingShift
{
    public partial class FrmBreedingShift : Form
    {
        public static string VmapXmlFile = "";  //VMAP.XMLのファイル名
        public static string MMD500最終更新日 = "";
        public static long ShiftDay = 0;        //シフトする日数
        public static string strFileName = "";
        Func<string, string> oleDB = a => Validation.OLDDB_テンプレート.Replace("?", a);

        public FrmBreedingShift() {
            InitializeComponent();
        }
        List<string> IDX = new List<string>();

        FrmWait Frm = null; //new FrmWait();
        private void FrmBreedingShift_Load(object sender, EventArgs e) {
            Action<string> act = a => System.Diagnostics.Debug.WriteLine(a);
            // TODO: このコード行はデータを 'breedingDataSet.繁殖情報' テーブルに読み込みます。必要に応じて移動、または削除をしてください。
            //this.繁殖情報TableAdapter.Fill(this.breedingDataSet.繁殖情報);
            //FpSpd基本のカラムラベルインデックス(IDX)テーブルを作成 
            for (int i = 0; i < FpSpd繁殖情報.Sheets[0].ColumnCount; i++) {
                IDX.Add(FpSpd繁殖情報.Sheets[0].Columns[i].Tag.ToString());
                act(FpSpd繁殖情報.Sheets[0].GetColumnLabel(0, i) + "   " + FpSpd繁殖情報.Sheets[0].Columns[i].Tag);
            }
            //キー（HKEY_CURRENT_USER\Software\test\sub）を開く
            //----------------------------------
            FpSpd繁殖情報.Sheets[0].Columns["農家コード"].Visible = false;
            FpSpd繁殖情報.Sheets[0].Columns["個体番号"].Visible = false;
            FpSpd繁殖情報.Sheets[0].Columns["N10"].Visible = false;
            FpSpd繁殖情報.Sheets[0].Columns["牛床番号"].SortIndicator = FarPoint.Win.Spread.Model.SortIndicator.Ascending;
            FpSpd繁殖情報.ActiveSheet.AutoFilterMode = FarPoint.Win.Spread.AutoFilterMode.EnhancedContextMenu;
            FpSpd繁殖情報.SetCursor(FarPoint.Win.Spread.CursorType.Normal, Cursors.Arrow);
            FpSpd繁殖情報.Sheets[0].AutoGenerateColumns = false;    //和暦できないのでいれてみた(1)
            //----------------------------------
            if (strFileName == "") {
                Microsoft.Win32.RegistryKey regkey =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\ORION\VMAP", false);
                if (regkey != null) {
                    VmapXmlFile = (string)regkey.GetValue("DataPath", "") + @"\VMAP.xml";
                    Properties.Settings.Default["BreedingConnectionString"] = oleDB(new BreedingTable_Make().databaseNameA);
                    //System.Diagnostics.Debug.WriteLine(Properties.Settings.Default["BreedingConnectionString"]);
                }
            }
            btn再読込_Click(this, EventArgs.Empty);
        }

        private void FrmBreedingShift_Shown(object sender, EventArgs e) {
            if (MessageBox.Show(this,"!!注意!!\n実行する前に必ず『VMAPバックアップ』を実行してください。\n続けますか", "実行まえの確認事項", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No) {
                this.Close();
            }
        }

        private void btn再読込_Click(object sender, EventArgs e) {
            // TODO: このコード行はデータを 'breedingDataSet.基本' テーブルに読み込みます。必要に応じて移動、または削除をしてください。
            this.繁殖情報TableAdapter.Fill(this.breedingDataSet.繁殖情報);
            分娩後日数更新();
            FpSpd繁殖情報BackColor();
            if (BasePrimKeyUpd.xmlRead("Settings", "Format", "日付") == "和暦") {
                Cls和暦.和暦表示(FpSpd繁殖情報);
            }
            MMD500最終更新日 = MilkingMaxDate();
            if (MMD500最終更新日 != "") {
                label2.Text = "最終搾乳日";
            }
            else {
                MMD500最終更新日 = DateTime.Today.ToString();
            }
            lblBaseDate.Text = MMD500最終更新日;
        }
        private void FpSpd繁殖情報BackColor() {
            Dictionary<string, string> DictBun = new Dictionary<string, string>(){
                {"育成牛", "#D2CCE6"}, {"未授精牛", "#BBD9F2"}, {"妊鑑待牛", "#DEEBBA"}, {"空胎牛", "#C4E5E3"},
                {"妊娠牛", "#FCD7A1"}, {"乾乳牛", "#FFF9B1"}, {"除籍予定牛", "#DCDDDD"}
            };
            Func<string, string> dict = a => DictBun.ContainsKey(a) ? DictBun[a] : "#FFFFFF";
            Func<string, int> iX = a => IDX.IndexOf(a);
            for (int i = 0; i < FpSpd繁殖情報.Sheets[0].Rows.Count; i++) {
                FpSpd繁殖情報.Sheets[0].Rows[i].BackColor = ColorTranslator.FromHtml(dict(FpSpd繁殖情報.Sheets[0].Cells[i, iX("分類名")].Text));
            }
        }

        private void lblBaseDate_TextChanged(object sender, EventArgs e) {
            TimeSpan span = DateTime.Today - DateTime.Parse(lblBaseDate.Text);
            txt日数.Text = span.Days.ToString();
        }
        Action<FarPoint.Win.Spread.Cell, int> Acf = (obj, v) => {
            if ((DateTime?)obj.Value != null) obj.Value = ((DateTime)obj.Value).AddDays(v);
        };

        private void btn日付変更_Click(object sender, EventArgs e) {
            if (MessageBox.Show("基準日と今日の日付の相対日数で繁殖情報を変更します。\nよろしいですか", "実行します。", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes) {
                日付シフト();
                分娩後日数更新();
            }
        }

        private void 日付シフト() {
            Func<string, int> iX = a => IDX.IndexOf(a);
            int _dy = Convert.ToInt16(txt日数.Text);
            List<int> item = new List<int>() { iX("分娩日"), iX("最終授精日"), iX("乾乳日"), iX("生年月日"), iX("F予定日"), 
                iX("発情1予定"), iX("発情2予定"), iX("発情3予定"), iX("妊鑑予定日"), iX("乾乳予定日"), iX("分娩予定日") };
            for (int i = 0; i < FpSpd繁殖情報.Sheets[0].Rows.Count; i++) {
                foreach (int j in item) {
                    Acf(FpSpd繁殖情報.Sheets[0].Cells[i, j], _dy);
                }
            }
            DateTime b = DateTime.Parse(lblBaseDate.Text).AddDays(_dy);
            lblBaseDate.Text = DateTime.Parse(lblBaseDate.Text).AddDays(_dy).ToString();
        }
        private void 分娩後日数更新() {
            Func<string, int> iX = a => IDX.IndexOf(a);
            for (int i=0; i < FpSpd繁殖情報.Sheets[0].Rows.Count; i++) {
                if (FpSpd繁殖情報.Sheets[0].Cells[i, iX("分娩日")].Text != "") {
                    TimeSpan ts  = DateTime.Today - (DateTime)FpSpd繁殖情報.Sheets[0].Cells[i, iX("分娩日")].Value;
                    FpSpd繁殖情報.Sheets[0].Cells[i, iX("分娩後日数")].Text = ts.Days.ToString(); 
                }
            }
        }

        private void btn更新_Click(object sender, EventArgs e) {
            if (this.Modal) {

            }
            if (MessageBox.Show("基本情報および、繁殖情報を更新します。\nよろしいですか", "実行します。", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes) {
                ShiftDay = Validation.DateDiff(Validation.DateInterval.Day, DateTime.Parse(MMD500最終更新日), DateTime.Parse(lblBaseDate.Text));
                /*http://okwakatta.net/topic/topic023.html*/
                if (Frm == null || Frm.IsDisposed) {
                    Frm = new FrmWait();
                    FrmWait.FrmInstance = Frm;
                    
                }
                Frm.DoWorkEvent = Work;
                Frm.RunWorkerCompletedEvent = End;
                //Frm.ShowDialog(this);
                Frm.Show(this);
            }
        }
        private void Work(object sender, DoWorkEventArgs e) {
            List <BasePrimKeyUpd> tableLst = new List<BasePrimKeyUpd>() {
                new BreedingTable_Make(),
                new FeedTableMake(),
                new MilkingTableMake()
            };
            BackgroundWorker bgWorker = (BackgroundWorker)sender;
            try {
                int i=0;
                int skip = 100/tableLst.Count;
                foreach (var a in tableLst) {
                    if (bgWorker.CancellationPending) { //キャンセル?
                        e.Cancel = true;
                        break;
                    }
                    //Frm.msgText = a.databaseNameA + "更新中です。";
                    a.DBaseUPD();
                    bgWorker.ReportProgress((++i)*skip);
                }
                //Frm.msgText = "更新終了しました。";
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.Message + " " + ex.Source);
                e.Result = "データ変換中にエラーが発生しました";
            }
        }

        private void End(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                MessageBox.Show("キャンセルされました", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Result != null) {
                MessageBox.Show(e.Result.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else {
                MessageBox.Show("データ変換が完了しました", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            Frm.FormClose();
        }

        /// <summary>
        /// MMD500の最終搾乳日付を取得する
        /// </summary>
        /// <param name="ph"></param>
        /// <returns></returns>
        private string MilkingMaxDate() {
            string d = "";
            string MilkingFileName = oleDB(new MilkingTableMake().databaseNameA);
            using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(MilkingFileName))
            using (DataSet dSet = new DataSet("t_1st乳量"))
            using (OleDbDataAdapter dAdp = new OleDbDataAdapter("SELECT MAX(搾乳日付) AS 最新日 FROM 1S乳量1;",con)) {
                try {
                    dAdp.Fill(dSet, "t_1st乳量");
                    DataTable dTbl = dSet.Tables["t_1st乳量"];
                    d = dTbl.Rows[0]["最新日"].ToString();
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message + ex.Source, "基本", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return d;
        }

        private void btn印刷_Click(object sender, EventArgs e) {
            PrintPreviewDialog ppd = new PrintPreviewDialog();
            //((Form)ppd).WindowState = FormWindowState.Maximized;
            ((Form)ppd).Left = this.Left;
            ((Form)ppd).Top = this.Top;
            ((Form)ppd).Width = this.Width;
            ((Form)ppd).Height = this.Height;
            FpSpd繁殖情報.SetPrintPreview(ppd);

            FarPoint.Win.Spread.PrintInfo pi = new FarPoint.Win.Spread.PrintInfo();
            pi.ShowPrintDialog = true;
            pi.JobName = "繁殖データ、予定日のシフトツール　プレビュー";
            pi.Header = "/c繁殖情報印刷";
            pi.Header += "/r日付:/ds/n　";
            pi.Footer = "/c- /p -";
            //pi.AbortMessage = "Do you want to cancel the printing?";
            pi.Margin.Top = 100;
            pi.Margin.Left = 100;
            pi.Margin.Right = 100;
            pi.Margin.Bottom = 100;
            pi.SmartPrintRules.Clear();
            pi.Preview = true;

            pi.Orientation = FarPoint.Win.Spread.PrintOrientation.Landscape; //横向き
            // Assign the printer settings to the sheet and print it
            FpSpd繁殖情報.Sheets[0].PrintInfo = pi;
            FpSpd繁殖情報.PrintSheet(FpSpd繁殖情報.ActiveSheetIndex);
        }
    }

    /******************************************************/
    /* プライムキーに日付を使用するテーブルの更新         */
    /* ①ワークフテーブルの削除                           */
    /* ②ターゲットテーブルをワークテーブルにコピーする   */
    /* ③ワークファイルの日付編集                         */
    /* ③ターゲットテーブルを空にする                     */
    /* ④ワークテーブルをターゲットテーブルにコピーする   */
    /******************************************************/
    public abstract class BasePrimKeyUpd
    {
        protected Func<string, string> fnc = a => @", DateAdd('d'," + FrmBreedingShift.ShiftDay + ", " + a + ") AS " + a;
        Func<string, string> oleDB = a => Validation.OLDDB_テンプレート.Replace("?", a);
        public abstract string databaseNameA { get; }
      //public abstract string databaseName { get; }
        protected abstract List<dbItemTbl> dbItemTbls { get; }
        protected const string tmpTable = "WORKTABLE";

        protected static Xml xL = new Xml(FrmBreedingShift.VmapXmlFile);
        public static Func<string, string, string, string> xmlRead = (a, b, c) => xL.XmlRead(a, b, c);

        public virtual void DBaseUPD() {
            this.DBasePatch();
            this.BaseDBaseUPD();
        }
        protected virtual void DBasePatch() { }

        protected virtual void BaseDBaseUPD() {
            Action<string> act = a => System.Diagnostics.Debug.WriteLine(a);

            using (System.Data.OleDb.OleDbConnection con = new OleDbConnection(oleDB(databaseNameA)))
            using (OleDbCommand cmd = new OleDbCommand()) {
                OleDbTransaction transaction = null;
                
                // Set the Connection to the new OleDbConnection.
                cmd.Connection = con;

                // Open the connection and execute the transaction.
                try {
                    con.Open();

                    // Start a local transaction with ReadCommitted isolation level.
                    transaction = con.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                    
                    // Assign transaction object for a pending local transaction.
                    cmd.Connection = con;
                    cmd.Transaction = transaction;
                    
                    foreach (dbItemTbl dbi in dbItemTbls) {
                        // Open the connection and execute the transaction.
                        DataTable dt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                        //ワークテーブルの削除
                        DataRow[] dr = dt.AsEnumerable().Where(x => x.Field<string>("TABLE_NAME").Equals(tmpTable)).ToArray();
                        if (dr.Count() > 0) {
                            cmd.CommandText = "DROP TABLE " + tmpTable; //削除
                            act("DROP TABLE " + tmpTable);
                            cmd.ExecuteNonQuery();
                        }
                        cmd.CommandText = dbi.SQL_Select;
                        act(cmd.CommandText);
                        cmd.ExecuteNonQuery();  //対象テーブルを日付を変更してtmpTableにコピーする

                        cmd.CommandText = dbi.SQL_Delete;    //strBuf = @"DELETE FROM " + dbi.TabelName;
                        act(cmd.CommandText);
                        cmd.ExecuteNonQuery();  //対象テーブルを全件削除する。

                        cmd.CommandText = dbi.SQL_Insert;    //strBuf = @"INSERT INTO " + dbi.TabelName + @" SELECT * FROM " + tmpTable;
                        act(cmd.CommandText);
                        cmd.ExecuteNonQuery();  //tmpTable を対象テーブルにコピー（移動） 
                        ExtDBaseUPD(cmd);
                    }
                    transaction.Commit();
                    con.Close();
                    EditDaoDB2.DB最適化(databaseNameA);

                }
                catch (Exception ex) {
                    Validation.ErrMsg(ex, "BasePrimKeyUpd");
                    // Attempt to roll back the transaction.
                    transaction.Rollback();
                    throw;
                }
                // The connection is automatically closed when the
                // code exits the using block.
            }
        }
        protected virtual void ExtDBaseUPD(OleDbCommand cmd) {
        }
    }

    public class dbItemTbl
    {
        public string TabelNameA { get; set; }       //テーブル名
        public string SQL_Select { get; set; }       //
        public string SQL_Delete { get; set; }
        public string SQL_Insert { get; set; }
    }
    /**********************************************/
    /* breeding.accDBの更新                       */
    /**********************************************/
    public class BreedingTable_Make : BasePrimKeyUpd
    {
        public override string databaseNameA {
            get {
                return xL.XmlRead("DataBase", "Path", "Breeding");
            }
        }
        /*public override string databaseName {
            get {
                return Validation.OLDDB_テンプレート.Replace("?", databaseNameA);
            }
        }*/
        protected override List<dbItemTbl> dbItemTbls {
            get {
                return new List<dbItemTbl>() {
                    new dbItemTbl(){
                        TabelNameA = "繁殖成績", 
                        SQL_Select = @"SELECT 繁殖成績.農家コード, 繁殖成績.個体番号, 繁殖成績.産次" + fnc("分娩日") + ", 分娩状態, 繁殖成績.性別, 分娩メモ "+ fnc("フレッシュ日付") + ", フレッシュ状態, フレッシュ所見, " +
                            @"初回発情日, 最終発情日, 初回授精日" + fnc("最終授精日") + ", 初授精日数, 授精回数, 発情状態, 卵巣, 種牛略号, " + 
                            @"繁殖形態, 妊鑑"  + fnc("妊鑑日") + fnc("乾乳日") + ", 空胎日数, 搾乳日数, 乾乳日数, 分娩間隔, VWP, VWP経過日数,JMR,妊否 " +
                            @"INTO WORKTABLE FROM 繁殖成績 " + 
                            @"INNER JOIN 基本 ON (繁殖成績.産次 = 基本.産次) AND (繁殖成績.個体番号 = 基本.個体番号) AND (繁殖成績.農家コード = 基本.農家コード) WHERE 基本.農家コード = 1",
                        SQL_Delete = @"DELETE 繁殖成績.* FROM 繁殖成績 LEFT JOIN 基本 ON 繁殖成績.[個体番号] = 基本.[個体番号] AND 繁殖成績.[産次] = 基本.[産次] WHERE 基本.農家コード = 1",
                        SQL_Insert = @"INSERT INTO 繁殖成績 SELECT * FROM " + tmpTable,
                    },
                    new dbItemTbl(){
                        TabelNameA = "農作業明細", 
                        SQL_Select = @"SELECT 農作業明細.農家コード, 農作業明細.個体番号, 農作業明細.産次 " + fnc("日付") + ", 種別, 回数, 農作業明細.分娩後日数, 発情状態, 卵巣, 種牛略号, 授精師, 処置, 繁殖形態, 妊鑑, メモ" + fnc("妊鑑日") +
                            @" INTO WORKTABLE FROM 農作業明細 " + 
                            @"INNER JOIN 基本 ON (農作業明細.産次 = 基本.産次) AND (農作業明細.個体番号 = 基本.個体番号) AND (農作業明細.農家コード = 基本.農家コード) WHERE 基本.農家コード = 1",
                        SQL_Delete = @"DELETE 農作業明細.* FROM 農作業明細 LEFT JOIN 基本 ON 農作業明細.[個体番号] = 基本.[個体番号] AND 農作業明細.[産次] = 基本.[産次] WHERE 基本.農家コード = 1",
                        SQL_Insert = @"INSERT INTO 農作業明細 SELECT * FROM " + tmpTable,
                    },
                    new dbItemTbl(){
                        TabelNameA = "基本", 
                        SQL_Select = @"SELECT 農家コード, 個体番号, 性別, 検定番号, 産次, 分娩後日数, 登録番号, 共済番号, 予備番号, 牛床番号, 群番号, 名号, 生年月日, 初産月齢, 品種, 毛色, 転入日付, 転入産次, 転入メモ, " +
                            @"除籍日, 除籍区分, 除籍内容, 売却先, 売却金額, 売却メモ, 分類, 分類名, 繁殖種別," +
                            @"父牛略号, 父牛名号, 父牛備考, 母牛番号, 母牛産次, 母牛名号, 母牛備考, 祖父牛略号, 祖父牛名号, 祖父牛備考, 祖母牛番号, 祖母牛名号, 祖母牛備考, " +
                            @"前回搾乳開始日時, 前回搾乳量, 前回電気伝導度, 前回乳温, 注意分房左前, 注意分房左後, 注意分房右前, 注意分房右後" + 
                            fnc("F予定日") + fnc("発情1予定") + fnc("発情2予定") + fnc("発情3予定") + fnc("妊鑑予定日")  + fnc("乾乳予定日") + fnc("分娩予定日") + ", 画像1, 画像2, BCS ,搾乳禁止自,搾乳禁止至 " +
                            @"INTO WORKTABLE FROM 基本 WHERE 農家コード = 1",
                        SQL_Delete = @"DELETE FROM 基本 WHERE 農家コード = 1",
                        SQL_Insert = @"INSERT INTO 基本 SELECT * FROM " + tmpTable,
                    }
                };
            }
        }
    }

    /**********************************************/
    /* feed.accDBの更新                           */
    /**********************************************/
    public class FeedTableMake : BasePrimKeyUpd
    {
        public override string databaseNameA {
            get {
                return xL.XmlRead("DataBase", "Path", "Feeding");
            }
        }
        /*public override string databaseName {
            get {
                return Validation.OLDDB_テンプレート.Replace("?", databaseNameA);
            }
        }*/
        protected override List<dbItemTbl> dbItemTbls {
            get {
                return new List<dbItemTbl>() {
                    new dbItemTbl(){
                        TabelNameA = "搾乳回数別乳量",
                        SQL_Select = @"SELECT 農家コード, 牛床番号, 個体番号" + fnc("搾乳日付") + ", " +
                            @"搾1_通信ユニット番号, 搾1_MMD500" + fnc("搾1_搾乳開始時間") + fnc("搾1_搾乳終了時間") + ", 搾1_乳量, 搾1_手入力乳量, 搾1_流量ピーク, 搾1_流量平均, 搾1_搾乳時間, 搾1_EC, 搾1_乳温, 搾1_注意分房A, 搾1_注意分房B, 搾1_注意分房C, 搾1_注意分房D, 搾1_異常Flag, 搾1_1h乳量, 搾1_乳量判定値, 搾1_EC判定値, 搾1_乳温判定値, 搾1_平均搾乳時間, " +
                            @"搾2_通信ユニット番号, 搾2_MMD500" + fnc("搾2_搾乳開始時間") + fnc("搾2_搾乳終了時間") + ", 搾2_乳量, 搾2_手入力乳量, 搾2_流量ピーク, 搾2_流量平均, 搾2_搾乳時間, 搾2_EC, 搾2_乳温, 搾2_注意分房A, 搾2_注意分房B, 搾2_注意分房C, 搾2_注意分房D, 搾2_異常Flag, 搾2_1h乳量, 搾2_乳量判定値, 搾2_EC判定値, 搾2_乳温判定値, 搾2_平均搾乳時間, " +
                            @"搾3_通信ユニット番号, 搾3_MMD500" + fnc("搾3_搾乳開始時間") + fnc("搾3_搾乳終了時間") + ", 搾3_乳量, 搾3_手入力乳量, 搾3_流量ピーク, 搾3_流量平均, 搾3_搾乳時間, 搾3_EC, 搾3_乳温, 搾3_注意分房A, 搾3_注意分房B, 搾3_注意分房C, 搾3_注意分房D, 搾3_異常Flag, 搾3_1h乳量, 搾3_乳量判定値, 搾3_EC判定値, 搾3_乳温判定値, 搾3_平均搾乳時間, " +
                            @"日乳量, 計算乳量 INTO WORKTABLE FROM 搾乳回数別乳量 ", 
                        SQL_Delete = @"DELETE FROM 搾乳回数別乳量",
                        SQL_Insert = @"INSERT INTO 搾乳回数別乳量 SELECT * FROM " + tmpTable,
                    },
                    new dbItemTbl(){
                        TabelNameA = "月乳量",
                        SQL_Select = "SELECT * INTO WORKTABLE FROM QMilk", 
                        SQL_Delete = @"DELETE FROM 月乳量",
                        SQL_Insert = @"INSERT INTO 月乳量 SELECT * FROM " + tmpTable,
                    },
                    new dbItemTbl(){
                        TabelNameA = "搾乳エラー",
                        SQL_Select = @"SELECT 農家コード, 異常Flag, 牛床番号, 個体番号" + fnc("搾乳日付") + ", 搾乳回数, 通信ユニット番号, MMD500, MMD表示番号" +
                            fnc("搾乳開始時間") + fnc("搾乳終了時間") + ", 乳量, 流量ピーク, 流量平均, 搾乳時間, EC, 乳温, 注意分房A, 注意分房B, 注意分房C, 注意分房D " +
                            @"INTO WORKTABLE FROM 搾乳エラー ", 
                        SQL_Delete = @"DELETE FROM 搾乳エラー",
                        SQL_Insert = @"INSERT INTO 搾乳エラー SELECT * FROM " + tmpTable,
                    }
                };
            }
        }
        //TRANSFORMは、INTOでテーブル作成できないのでQueryで対応
        protected override void DBasePatch() {
            string sql = "TRANSFORM Sum(搾乳回数別乳量.[日乳量]) AS 日乳量の合計 " +
                @"SELECT 搾乳回数別乳量.[農家コード],搾乳回数別乳量.[個体番号], year(搾乳日付) AS 年 " +
                @"FROM 搾乳回数別乳量 " +  //WHERE year(搾乳日付)=2016 " +
                @"GROUP BY 搾乳回数別乳量.[農家コード], 搾乳回数別乳量.[個体番号], year(搾乳日付) " +
                @"PIVOT '乳量' & month([搾乳日付]) In ('乳量1','乳量2','乳量3','乳量4','乳量5','乳量6','乳量7','乳量8','乳量9','乳量10','乳量11','乳量12')";
            EditDaoDB2.QueryDef(xL.XmlRead("DataBase", "Path", "Feeding"), "QMilk", sql);
        }
    }

    /**********************************************/
    /* MDD500.accDBの更新                         */
    /**********************************************/
    public class MilkingTableMake : BasePrimKeyUpd
    {
        public override string databaseNameA {
            get {
                return xL.XmlRead("DataBase", "Path", "Milking");
            }
        }
        /*public override string databaseName {
            get {
                return Validation.OLDDB_テンプレート.Replace("?", databaseNameA);
            }
        }*/
        protected override List<dbItemTbl> dbItemTbls {
            get {
                return new List<dbItemTbl>() {
                    new dbItemTbl(){
                        TabelNameA = "1S乳量1",
                        SQL_Select = @"SELECT 農家コード, 個体番号, 牛番号, 牛床番号, " +
                            @"通信ユニット番号, MMD番号" + fnc("搾乳日付") + ", 搾乳回数" + fnc("取込日時") + ", 積算乳量, 流量, 搾乳時間, EC, 乳温, 注意分房A, 注意分房B, 注意分房C, 注意分房D, 離脱情報, 異常Flag, 重複Flag " +
                            @"INTO WORKTABLE FROM 1S乳量1 ", 
                        SQL_Delete = @"DELETE FROM 1S乳量1",
                        SQL_Insert = @"INSERT INTO 1S乳量1 SELECT * FROM " + tmpTable,
                    },
                    new dbItemTbl(){
                        TabelNameA = "1S乳量2",
                        SQL_Select = @"SELECT 農家コード, 個体番号, 牛番号, 牛床番号, " +
                            @"通信ユニット番号, MMD番号" + fnc("搾乳日付") + ", 搾乳回数" + fnc("取込日時") + ", 積算乳量, 流量, 搾乳時間, EC, 乳温, 注意分房A, 注意分房B, 注意分房C, 注意分房D, 離脱情報, 異常Flag, 重複Flag " +
                            @"INTO WORKTABLE FROM 1S乳量2 ", 
                        SQL_Delete = @"DELETE FROM 1S乳量2",
                        SQL_Insert = @"INSERT INTO 1S乳量2 SELECT * FROM " + tmpTable,
                    }
                };
            }
        }
    }
}
