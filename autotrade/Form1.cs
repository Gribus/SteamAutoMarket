﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace autotrade {
    public partial class Form1 : Form {
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        public Form1() {
            InitializeComponent();
            sidePanel.Height = saleLinkButton.Height;
            sidePanel.Top = saleLinkButton.Top;
        }

        private void appExitButton_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void panel2_Paint(object sender, PaintEventArgs e) {

        }

        private void leftPanelHideShowButton_Click(object sender, EventArgs e) {
            //913; 632
            if (this.Height == 632) {
                leftHeaderPanel.Width = 45;
                leftPanelHideShowButton.Left = 10;


                ////buy controll size
                //buyControl1.Width = 865;
                //buyControl1.Left = 10;

                ////sale controll size
                //saleControl1.Width = 865;
                //saleControl1.Left = 10;

                this.Width = 400;
            }
            else {
                leftHeaderPanel.Width = 166;
                leftPanelHideShowButton.Left = 128;
                //buy controll size
                //buyControl1.Width = 742;
                //buyControl1.Left = 168;
                //sale controll size
                //saleControl1.Width = 742;
                //saleControl1.Left = 168;
            }




        }

        private void saleLinkButton_Click(object sender, EventArgs e) {
            sidePanel.Height = saleLinkButton.Height;
            sidePanel.Top = saleLinkButton.Top;
            //saleControl1.BringToFront();

        }

        private void buyLinkButton_Click(object sender, EventArgs e) {
            sidePanel.Height = buyLinkButton.Height;
            sidePanel.Top = buyLinkButton.Top;
            //buyControl1.BringToFront();
        }

        //enabled move work space application
        private void move_MouseDown(object sender, MouseEventArgs e) {
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void move_MouseMove(object sender, MouseEventArgs e) {
            if (dragging) {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void move_MouseUp(object sender, MouseEventArgs e) {
            dragging = false;
        }

        private void appExpandButton_Click(object sender, EventArgs e) {
            if (this.WindowState != FormWindowState.Maximized) {
                this.WindowState = FormWindowState.Maximized;
            }
            else {
                this.WindowState = FormWindowState.Normal;
            }

        }

        private void appCurtailButton_Click(object sender, EventArgs e) {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}