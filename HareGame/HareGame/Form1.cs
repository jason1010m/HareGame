using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HareGame
{
    public partial class Form1 : Form
    {
        int type;  //己方代表棋
        int rx, ry;  //兔棋位置
        int dErrorMove;  //犬棋連續垂直走步次數
        byte[,] S; //對應棋盤狀態的陣列：0為空格，1為兔子，2為獵犬，3為兔子選擇中，4為獵犬選擇中，5為可行動格，6無此格
        Socket T; //通訊物件
        Thread Th; //網路監聽執行緒
        string User;//使用者
        Image ok = new Bitmap("piece_ok.png");       //宣告及讀取可選位置圖
        RabbitPiece rPiece = new RabbitPiece();
        DogPiece dPiece = new DogPiece();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            S = new byte[3, 5] { { 6, 0, 0, 2, 6 } ,{ 1, 0, 0, 0, 2 },{ 6, 0, 0, 2, 6 }}; //宣告及設置棋盤資訊陣列
            rx = 0;ry = 1;  //初始兔棋位置
            type = 0;  //代表棋未定
            dErrorMove = 0;  //初始犬棋連續垂直走步
            drawAll();  //繪製所有物件
        }
        //遊戲勝負判斷
        private int gameWin() //0為勝負未定，1為兔贏，2為犬贏
        {
            Boolean f=true;
            //判斷犬棋連續走步次數
            if (dErrorMove >= 10) return 1; 
            //判斷兔棋是否超越所有犬棋
            for (int i = 0; i < 3; i++)  
            {
                for (int j = 0; j < 5; j++)
                {
                    if (S[i, j] == 2)
                    {
                        if ( rx<j ) { f = false; }
                    }
                }
            }
            if (f == true) return 1;
            //判斷兔棋能否行走
            if (ry - 1 >= 0) { if (S[ry - 1, rx] == 0) return 0; }
            if (ry + 1 <= 2) { if (S[ry + 1, rx] == 0) return 0; }
            if (rx - 1 >= 0) { if (S[ry, rx - 1] == 0) return 0; }
            if (rx + 1 <= 4) { if (S[ry, rx + 1] == 0) return 0; }
            if (!((ry % 2 == 0 && rx % 2 == 0) || (ry % 2 == 1 && rx % 2 == 1)))
            {
                if (ry - 1 >= 0 && rx - 1 >= 0) { if (S[ry - 1, rx - 1] == 0) return 0; }
                if (ry + 1 <= 2 && rx + 1 <= 4) { if (S[ry + 1, rx + 1] == 0) return 0; }
                if (rx - 1 >= 0 && ry + 1 <= 2) { if (S[ry + 1, rx - 1] == 0) return 0; }
                if (rx + 1 <= 4 && ry - 1 >= 0) { if (S[ry - 1, rx + 1] == 0) return 0; }
            }
            return 2;
        }

        private void drawAll()
        {
            Bitmap bg = new Bitmap("Hare_board.png"); //棋盤影像物件
            Graphics g = Graphics.FromImage(bg); //棋盤影像繪圖物件
            panel1.BackgroundImage = bg; //貼上棋盤影像為panel1的背景
            for (int i = 0; i < 3; i++)
            {
                for(int j = 0; j < 5; j++)
                {
                    //依照棋盤資訊繪製不同圖片(1為兔棋，2為犬棋，3為兔棋選擇中，4為犬棋選擇中，5為可行動格提示
                    if (S[i, j] == 1) { rPiece.draw(g, j, i,false); }
                    if (S[i, j] == 2) { dPiece.draw(g, j, i,false); }
                    if (S[i, j] == 3) { rPiece.draw(g, j, i, true); }
                    if (S[i, j] == 4) { dPiece.draw(g, j, i, true); }
                    if (S[i, j] == 5) { g.DrawImage(ok, 40 + j * 160, 40 + i * 160 , 80, 80); }
                }
            }
            Refresh();      //刷新
        }

        //下棋的動作
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            int x = (e.X-30)/60; //算出是第幾個水平向的棋格
            int y = (e.Y-30)/60; //算出是第幾個垂直向的棋格
            if ((x % 2 == 0) && (y % 2 == 0))   //判斷正確棋格
            {
                x /= 2;y /= 2;      //確定棋格位置
                //選擇兔棋
                if (S[y , x] == 1)
                {
                    //若己方棋為兔棋
                    if (type == 1)
                    {
                        S[y, x] = 3;    //設置棋盤資訊為選擇中
                        //判斷可行動格有哪些並設置棋盤資訊
                        if (y - 1 >= 0) { if (S[y - 1, x] == 0) S[y - 1, x] = 5; }
                        if (y + 1 <= 2) { if (S[y + 1, x] == 0) S[y + 1, x] = 5; }
                        if (x - 1 >= 0) { if (S[y, x - 1] == 0) S[y, x - 1] = 5; }
                        if (x + 1 <= 4) { if (S[y, x + 1] == 0) S[y, x + 1] = 5; }
                        if (!((y % 2 == 0 && x % 2 == 0) || (y % 2 == 1 && x % 2 == 1)))
                        {
                            if (y - 1 >= 0 && x - 1 >= 0) { if (S[y - 1, x - 1] == 0) S[y - 1, x - 1] = 5; }
                            if (y + 1 <= 2 && x + 1 <= 4) { if (S[y + 1, x + 1] == 0) S[y + 1, x + 1] = 5; }
                            if (x - 1 >= 0 && y + 1 <= 2) { if (S[y + 1, x - 1] == 0) S[y + 1, x - 1] = 5; }
                            if (x + 1 <= 4 && y - 1 >= 0) { if (S[y - 1, x + 1] == 0) S[y - 1, x + 1] = 5; }
                        }
                        drawAll();      //重新繪製所有物件
                        if (listBox1.SelectedIndex >= 0) //如果有對手時
                        {
                            Send("6" + x.ToString() + "," + y.ToString() + "|" + listBox1.SelectedItem);
                        }
                    }
                }
                //選擇犬棋
                if (S[y, x] == 2 )
                {
                    //若己方棋未定則拿棋並告知對方
                    if (type == 0) {
                        type = 2;
                        if (listBox1.SelectedIndex >= 0) //如果有對手時
                        {
                            Send("4" + "1" + "|" + listBox1.SelectedItem);
                        }
                    }
                    //若己方棋為犬棋
                    if (type == 2)
                    {
                        //由於犬棋有3個，因此每次選擇就將選擇中的棋子及可行動格提示刷掉
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                if (S[i, j] == 4) S[i, j] = 2;
                                if (S[i, j] == 5) S[i, j] = 0;
                            }
                        }
                        S[y, x] = 4;    //設置棋盤資訊為選擇中
                        //判斷可行動格有哪些並設置棋盤資訊
                        if (y - 1 >= 0) { if (S[y - 1, x] == 0) S[y - 1, x] = 5; }
                        if (y + 1 <= 2) { if (S[y + 1, x] == 0) S[y + 1, x] = 5; }
                        if (x - 1 >= 0) { if (S[y, x - 1] == 0) S[y, x - 1] = 5; }
                        if (!((y % 2 == 0 && x % 2 == 0) || (y % 2 == 1 && x % 2 == 1)))
                        {
                            if (y - 1 >= 0 && x - 1 >= 0) { if (S[y - 1, x - 1] == 0) S[y - 1, x - 1] = 5; }
                            if (x - 1 >= 0 && y + 1 <= 2) { if (S[y + 1, x - 1] == 0) S[y + 1, x - 1] = 5; }
                        }
                        drawAll();      //重新繪製所有物件
                        if (listBox1.SelectedIndex >= 0) //如果有對手時
                        {
                            Send("6" + x.ToString() + "," + y.ToString() + "|" + listBox1.SelectedItem);
                        }
                    }
                }
                //點擊可行動格
                if (S[y, x] == 5)
                {
                    //將可行動格提示刷掉
                    for(int i = 0; i < 3; i++)
                    {
                        for(int j = 0; j < 5; j++)
                        {
                            if (S[i, j] == 4) { 
                                //犬棋連續垂直走步累加或重置
                                if (j == x)
                                {
                                    dErrorMove += 1;
                                }
                                else
                                {
                                    dErrorMove = 0;
                                }
                            }
                            if (S[i, j] == 3 || S[i, j] == 4 || S[i, j] == 5) S[i, j] = 0;
                        }
                    }
                    S[y, x] = Convert.ToByte(type);     //更新棋盤資訊
                    //己方為兔棋則更新兔棋位置
                    if (type == 1)
                    {
                        rx = x; ry = y;
                    }
                    drawAll();      //重新繪製所有物件
                    Send("7" + x.ToString() + "," + y.ToString() + "|" + listBox1.SelectedItem);
                    switch (gameWin())
                    {
                        case 0:
                            textBox4.Text = "" + "\r\n";//清空系統提示
                            break;
                        case 1:
                            textBox4.Text = "兔子方勝利！" + "\r\n";//兔方勝利提示
                            break;
                        case 2:
                            textBox4.Text = "獵犬方勝利！" + "\r\n";//犬方勝利提示
                            break;
                    }
                    panel1.Enabled = false; //輪到對手，你不能繼續下棋
                }
            }
        }

        private void Send(string str)
        {
            byte[] B = Encoding.Default.GetBytes(str);
            T.Send(B, 0, B.Length, SocketFlags.None);
        }

        //監聽 Server 訊息 (Listening to the Server)
        private void Listen()
        {
            EndPoint ServerEP = (EndPoint)T.RemoteEndPoint; //Server 的 EndPoint
            byte[] B = new byte[1023]; //接收用的 Byte 陣列
            int inLen = 0; //接收的位元組數目
            string Msg; //接收到的完整訊息
            string St; //命令碼
            string Str; //訊息內容(不含命令碼)
            string[] D;
            int x;
            int y;
            while (true) //無限次監聽迴圈
            {
                try
                {
                    inLen = T.ReceiveFrom(B, ref ServerEP); //收聽資訊並取得位元組數
                }
                catch (Exception) //產生錯誤時
                {
                    T.Close(); //關閉通訊器
                    listBox1.Items.Clear(); //清除線上名單
                    MessageBox.Show("伺服器斷線了！"); //顯示斷線
                    button1.Enabled = true; //連線按鍵恢復可用
                    Th.Abort(); //刪除執行緒
                }
                Msg = Encoding.Default.GetString(B, 0, inLen); //解讀完整訊息
                St = Msg.Substring(0, 1); //取出命令碼 (第一個字)
                Str = Msg.Substring(1); //取出命令碼之後的訊息
                switch (St) //依命令碼執行功能
                {
                    case "L": //接收線上名單
                        listBox1.Items.Clear(); //清除名單
                        string[] M = Str.Split(','); //拆解名單成陣列
                        for (int i = 0; i < M.Length; i++)
                        {
                            listBox1.Items.Add(M[i]); //逐一加入名單
                        }
                        break;
                    case "4": //對手拿犬棋
                        D = Str.Split(','); //切割座標訊息
                        type = int.Parse(D[0]);
                        panel1.Enabled = false; //對手下好了，輪到你下棋
                        break;
                    case "5": //對手重置棋盤
                        S = new byte[3, 5] { { 6, 0, 0, 2, 6 }, { 1, 0, 0, 0, 2 }, { 6, 0, 0, 2, 6 } }; //宣告及設置棋盤資訊陣列
                        rx = 0; ry = 1;  //初始兔棋位置
                        type = 0;  //代表棋未定
                        dErrorMove = 0;  //初始犬棋連續垂直走步
                        textBox4.Text = "遊戲重置" + "\r\n";//重置提示
                        panel1.Enabled = true; //對手下好了，輪到你下棋
                        drawAll();  //繪製所有物件
                        break;
                    case "6": //對手選擇棋訊息
                        D = Str.Split(','); //切割座標訊息
                        x = int.Parse(D[0]); //水平向的棋格
                        y = int.Parse(D[1]); //垂直向的棋格
                        //根據己方棋種更新與繪製棋盤資訊至與對方相同
                        if (type == 1)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                for (int j = 0; j < 5; j++)
                                {
                                    if (S[i, j] == 4) S[i, j] = 2;
                                    if (S[i, j] == 5) S[i, j] = 0;
                                }
                            }
                            S[y, x] = 4;
                            if (y - 1 >= 0) { if (S[y - 1, x] == 0) S[y - 1, x] = 5; }
                            if (y + 1 <= 2) { if (S[y + 1, x] == 0) S[y + 1, x] = 5; }
                            if (x - 1 >= 0) { if (S[y, x - 1] == 0) S[y, x - 1] = 5; }
                            if (!((y % 2 == 0 && x % 2 == 0) || (y % 2 == 1 && x % 2 == 1)))
                            {
                                if (y - 1 >= 0 && x - 1 >= 0) { if (S[y - 1, x - 1] == 0) S[y - 1, x - 1] = 5; }
                                if (x - 1 >= 0 && y + 1 <= 2) { if (S[y + 1, x - 1] == 0) S[y + 1, x - 1] = 5; }
                            }
                        }
                        if (type == 2)
                        {
                            S[y, x] = 3;
                            if (y - 1 >= 0) { if (S[y - 1, x] == 0) S[y - 1, x] = 5; }
                            if (y + 1 <= 2) { if (S[y + 1, x] == 0) S[y + 1, x] = 5; }
                            if (x - 1 >= 0) { if (S[y, x - 1] == 0) S[y, x - 1] = 5; }
                            if (x + 1 <= 4) { if (S[y, x + 1] == 0) S[y, x + 1] = 5; }
                            if (!((y % 2 == 0 && x % 2 == 0) || (y % 2 == 1 && x % 2 == 1)))
                            {
                                if (y - 1 >= 0 && x - 1 >= 0) { if (S[y - 1, x - 1] == 0) S[y - 1, x - 1] = 5; }
                                if (y + 1 <= 2 && x + 1 <= 4) { if (S[y + 1, x + 1] == 0) S[y + 1, x + 1] = 5; }
                                if (x - 1 >= 0 && y + 1 <= 2) { if (S[y + 1, x - 1] == 0) S[y + 1, x - 1] = 5; }
                                if (x + 1 <= 4 && y - 1 >= 0) { if (S[y - 1, x + 1] == 0) S[y - 1, x + 1] = 5; }
                            }
                        }
                        drawAll();
                        break;
                    case "7": //對手下棋訊息
                        D = Str.Split(','); //切割座標訊息
                        x = int.Parse(D[0]); //水平向的棋格
                        y = int.Parse(D[1]); //垂直向的棋格
                        //根據己方棋種更新與繪製棋盤資訊至與對方相同
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                if (S[i, j] == 4)
                                {
                                    if (j == x)
                                    {
                                        dErrorMove += 1;
                                    }
                                    else
                                    {
                                        dErrorMove = 0;
                                    }
                                }
                                if (S[i, j] == 3 || S[i, j] == 4 || S[i, j] == 5) S[i, j] = 0;
                            }
                        }
                        if (type == 1) S[y, x] = 2;
                        if (type == 2)
                        {
                            S[y, x] = 1;
                            rx = x; ry = y;
                        }
                        drawAll();
                        switch (gameWin())
                        {
                            case 0:
                                textBox4.Text = "輪到你了" + "\r\n";//下棋提示
                                panel1.Enabled = true; //對手下好了，輪到你下棋
                                break;
                            case 1:
                                textBox4.Text = "兔子方勝利！" + "\r\n";//兔方勝利提示
                                break;
                            case 2:
                                textBox4.Text = "獵犬方勝利！" + "\r\n";//犬方勝利提示
                                break;
                        }
                        break;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (button1.Enabled == false)
            {
                Send("9" + User);
                T.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Control.CheckForIllegalCrossThreadCalls = false; //忽略跨執行緒操作的錯誤
            User = textBox3.Text; //使用者名稱
            string IP = textBox1.Text; //伺服器IP
            int Port = int.Parse(textBox2.Text); //伺服器Port
            try
            {
                IPEndPoint EP = new IPEndPoint(IPAddress.Parse(IP), Port);//建立伺服器端點資訊
                                                                          //建立TCP通訊物件
                T = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                T.Connect(EP); //連上Server的EP端點(類似撥號連線)
                Th = new Thread(Listen); //建立監聽執行緒
                Th.IsBackground = true; //設定為背景執行緒
                Th.Start(); //開始監聽
                textBox4.Text = "已連線伺服器！" + "\r\n"; Send("0" + User);//隨即傳送自己的 UserName 給 Server
                button1.Enabled = false; //讓連線按鍵失效，避免重複連線
            }
            catch
            {
                textBox4.Text = "無法連上伺服器！" + "\r\n";//連線失敗時顯示訊息
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            S = new byte[3, 5] { { 6, 0, 0, 2, 6 }, { 1, 0, 0, 0, 2 }, { 6, 0, 0, 2, 6 } }; //宣告及設置棋盤資訊陣列
            rx = 0; ry = 1;  //初始兔棋位置
            type = 0;  //代表棋未定
            dErrorMove = 0;  //初始犬棋連續垂直走步
            textBox4.Text = "遊戲重置" + "\r\n";//遊戲重置提示
            if (listBox1.SelectedIndex >= 0)
            {
                Send("5" + "C" + "|" + listBox1.SelectedItem);//送出清除訊息給對手
            }
            panel1.Enabled = true; //對手下好了，輪到你下棋
            drawAll();  //重新繪製所有物件
        }
    }
}
