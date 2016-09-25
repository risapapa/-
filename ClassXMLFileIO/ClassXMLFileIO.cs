using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace ClassXMLFileIO
{
    public class Xml
    {
        private string fl = "";
        XmlDocument xDoc = new XmlDocument();
        public Xml(string VmapFileName = "") {
            //if (File.Exists("VMAP.xml")) {
            if (VmapFileName != "") {
                fl = VmapFileName;
            }
            else {
                fl = "VMAP.xml";
            }
            if (File.Exists(fl)) {
              //xDoc.Load("VMAP.xml");    //XML宣言
                xDoc.Load(fl);    //XML宣言
            }
            else {
                XmlDeclaration xmd = xDoc.CreateXmlDeclaration("1.0", "shift-jis", "yes");    //XML宣言
                xDoc.AppendChild(xmd);
                XmlElement root = xDoc.CreateElement("VMAP");     //ルートノードの作成
                xDoc.AppendChild(root);   //xmlDocumentオブジェクトにルートノードを追加
              //xDoc.Save("VMAP.xml");
                xDoc.Save(fl);
            }
        }
        public void XmlWrite(string nodeElement1, string nodeElement2, string nodeElement3, string nt1) {
            XmlDocument xd = new XmlDocument();
          //if (File.Exists("VMAP.xml")) {
            if (File.Exists(fl)) {
              //xd.Load("VMAP.xml");    //XML宣言
                xd.Load(fl);    //XML宣言
            }
            //XML宣言作成
            XmlDeclaration xmd = xd.CreateXmlDeclaration("1.0", "shift-jis", "yes");    //XML宣言
            xd.AppendChild(xmd);
            XmlElement root = xd.CreateElement("VMAP");     //ルートノードの作成
            XmlNode xn1 = xd.CreateNode(XmlNodeType.Element, nodeElement1, "");   //子ノードの作成
            XmlNode xn2 = xd.CreateNode(XmlNodeType.Element, nodeElement2, "");   //孫ノードの作成
            XmlNode xn3 = xd.CreateNode(XmlNodeType.Element, nodeElement3, "");   //ひ孫ノードの作成
            XmlCharacterData xt = xd.CreateTextNode(nt1);   //孫ノードの値
            xn3.AppendChild(xt);    //孫ノードの値を孫ノードに追加
            xn2.AppendChild(xn3);    //孫ノードの値を孫ノードに追加
            xn1.AppendChild(xn2);    //孫ノードの値を孫ノードに追加

            root.AppendChild(xn1);  //子ノードをルートノードに追加
            xd.AppendChild(root);   //xmlDocumentオブジェクトにルートノードを追加
            Console.WriteLine(xd.OuterXml);
            //xd.Save("VMAP.xml");
            xd.Save(fl);
        }
        void NodeMake(string nodeElement1, string nodeElement2, string text = null) {
            XmlNodeList xl = xDoc.SelectNodes(nodeElement1 + "/" + nodeElement2);
            if (xl.Count == 0) {
                XmlNode Root = xDoc.SelectSingleNode(nodeElement1);
                XmlNode xn = xDoc.CreateNode(XmlNodeType.Element, nodeElement2, "");   //子ノードの作成
                Root.AppendChild(xn);
                if (text != null) {
                    XmlCharacterData xt = xDoc.CreateTextNode(text);   //孫ノードの値
                    xn.AppendChild(xt);    //孫ノードの値を孫ノードに追加
                }
            }
            else if (text != null) {
                xl[0].InnerText = text;
            }
        }
        public void XmlReWrite(string nodeElement1, string nodeElement2, string nodeElement3, string text) {
            NodeMake("VMAP", nodeElement1);
            NodeMake("VMAP/" + nodeElement1, nodeElement2);
            NodeMake("VMAP/" + nodeElement1 + "/" + nodeElement2, nodeElement3, text);
          //xDoc.Save("VMAP.xml");
            xDoc.Save(fl);
        }
        /// <summary>
        /// XMLファイル読込
        /// </summary>
        /// <param name="nodeElement1">エレメント1</param>
        /// <param name="nodeElement2">エレメント2</param>
        /// <param name="nodeElement3">エレメント3</param>
        /// <param name="初期値">ない場合の初期値</param>
        /// <returns></returns>
        public string XmlRead(string nodeElement1, string nodeElement2, string nodeElement3, string 初期値 = "") {
            string Text = "";
            //xPathを使って、表示するノードを選択する
            string ndPath = "VMAP/" + nodeElement1 + "/" + nodeElement2 + "/" + nodeElement3;
            XmlNodeList xl = xDoc.SelectNodes(ndPath);
            /*foreach (XmlNode xn in xl) {
                Console.WriteLine(xn.InnerText);
            }*/
            if (xl.Count > 0) {
                Text = xl[0].InnerText;
            }
            else {
                Text = 初期値;
            }
            return Text;
        }
    }
}
