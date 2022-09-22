using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HareGame
{
    class RabbitPiece
    {
        Image rabbit = new Bitmap("piece_blue.png");       //宣告及讀取兔棋圖

        public void draw(Graphics g,int x,int y,Boolean select)        //繪製棋子圖片
        {
            g.DrawImage(rabbit, 40+x*160, 40+y*160-(select?20:0), 80, 80);
        }
    }
}
