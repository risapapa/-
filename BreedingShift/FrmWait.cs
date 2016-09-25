using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BreedingShift
{
    public partial class FrmWait : System.Windows.Forms.Form
    {
        private BackgroundWorker _bgw = new BackgroundWorker();
        private DoWorkEventHandler _doWorkEvent = null;
        private RunWorkerCompletedEventHandler _runWorkerCompletedEvent = null;
        private ProgressChangedEventHandler _ProgessChangedEvent = null;    //追加
        #region プロパティ
        /// <summary>
        /// DoWorkEventを設定します。設定できるのは最初の一回だけです。
        /// </summary>
        public DoWorkEventHandler DoWorkEvent {
            set {
                if (_doWorkEvent == null) {
                    _doWorkEvent = value;
                    _bgw.DoWork += new DoWorkEventHandler(value);
                }
            }
        }
        /*追加 http://dobon.net/vb/dotnet/programing/displayprogress.html
        public ProgressChangedEventHandler ProgressEvent {
            set {
                if (_ProgessChangedEvent == null) {
                    _ProgessChangedEvent = value;
                    _bgw.ProgressChanged += new ProgressChangedEventHandler(value);
                }
            }
        }*/
        /*　わかんない
        delegate void msgTextDelegate();

        public void msgText(string TXT) {
            if (InvokeRequired) {
                Invoke(new msgTextDelegate(msgText));
            }
            label2.Text = "a";
        }*/

       /*
        public string msgText {
            set {
                label2.Text = value;
            }
            //有効ではないスレッド間の操作: コントロールが作成されたスレッド以外のスレッドからコントロール 'label2' がアクセスされました。
        }
        */ 
        /// <summary>
        /// RunWorkerCompletedEventを設定します。設定できるのは最初の一回だけです。
        /// </summary>
        public RunWorkerCompletedEventHandler RunWorkerCompletedEvent {
            set {
                if (_runWorkerCompletedEvent == null) {
                    _runWorkerCompletedEvent = value;
                    _bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(value);
                }
            }
        }
        #endregion

        /*****************/
        //http://dobon.net/vb/dotnet/form/accessanotherformdata.html
        private static FrmWait _FrmWaitInstance;
        public static FrmWait FrmInstance {
            get {
                return _FrmWaitInstance;
            }
            set {
                _FrmWaitInstance = value;
            }
        }
        /*****************/

        #region イベント
        /// <summary>
        /// Formを表示したときに発生するイベントです。
        /// </summary>
        /// <param name="sender">イベントが発生したオブジェクトです。</param>
        /// <param name="e">イベント データを格納している <see cref="EventArgs"/>。</param>
        /// <remarks>
        /// 本イベントでバックグラウンド処理を動作させます。
        /// http://dobon.net/vb/dotnet/programing/displayprogress.html
        /// </remarks>
        private void FrmWait_Load(object sender, EventArgs e) {
            try {
                if (_doWorkEvent != null && _runWorkerCompletedEvent != null) {
                    _bgw.WorkerSupportsCancellation = true; //キャンセルできるようにする
                    _bgw.WorkerReportsProgress = true;      //BackgroundWorkerのProgressChangedイベントが発生するようにする
                    _bgw.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker1_ProgressChanged);

                    // バックグラウンド操作の実行を開始します。
                    _bgw.RunWorkerAsync();
                }
                else {
                    // 設定していない場合は画面を閉じます。
                    Close();
                }
            }
            catch (System.Exception ex) {
                throw ex;
            }
        }
        #endregion

        /// <summary>
        /// コンストラクタです。
        /// </summary>
        public FrmWait() {
            // 閉じる、最小化、最大化ボタンを表示してません。
            // this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            InitializeComponent();
        }

        #region メソッド
        /// <summary>
        /// 本Formを閉じます。
        /// </summary>
        public void FormClose() {
            Close();
        }
        #endregion

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            //ProgressBar1の値を変更する
            progressBar1.Value = e.ProgressPercentage;
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            _bgw.CancelAsync();
        }
    }
}
