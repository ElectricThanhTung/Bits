﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace Bits {
    public partial class MainWindow : Window {
        private bool WindowShown = false;
        private bool RegisterEventEnabled = true;

        public MainWindow() {
            InitializeComponent();
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            Thread CalculationProcess = new Thread(CalculationHandler);
            CalculationProcess.IsBackground = true;
            CalculationProcess.Start();
        }

        override protected void OnContentRendered(EventArgs e) {
            if(!WindowShown) {
                WindowShown = true;
                expression_textbox.MaxWidth = expression_textbox.ActualWidth;
                interger_textbox.MaxWidth = interger_textbox.ActualWidth;
                hex_textbox.MaxWidth = hex_textbox.ActualWidth;
                result_textblock.MaxWidth = result_textblock.ActualWidth;
                this.InvalidateMeasure();
            }
            base.OnContentRendered(e);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
            if(e.GetPosition(this).Y < 30 && e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Register_ValueChanged(object sender) {
            if(RegisterEventEnabled) {
                uint value = reg0.Value;
                value |= (uint)reg1.Value << 8;
                value |= (uint)reg2.Value << 16;
                value |= (uint)reg3.Value << 24;
                expression_textbox.Text = value.ToString();
                expression_textbox.SelectionStart = expression_textbox.Text.Length;
            }
        }

        private void TextBlock_MouseDown(object sender, MouseEventArgs e) {
            ClrResult();
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e) {
            ((TextBlock)sender).Foreground = Brushes.Red;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e) {
            ((TextBlock)sender).Foreground = Brushes.Blue;
        }

        private void Button_CloseWindows(object sender, RoutedEventArgs e) {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.WindowState = WindowState.Minimized;
        }

        private void ClrResult() {
            this.Dispatcher.BeginInvoke(() => {
                interger_textbox.Text = "";
                hex_textbox.Text = "";
                expression_textbox.Text = "";
                result_textblock.Text = "0";
                result_textblock.Foreground = Brushes.Blue;
                RegisterEventEnabled = false;
                reg0.Value = 0;
                reg1.Value = 0;
                reg2.Value = 0;
                reg3.Value = 0;
                binary_text_0.Text = Calculator.ByteToBIN(reg0.Value);
                binary_text_1.Text = Calculator.ByteToBIN(reg1.Value);
                binary_text_2.Text = Calculator.ByteToBIN(reg2.Value);
                binary_text_3.Text = Calculator.ByteToBIN(reg3.Value);
                RegisterEventEnabled = true;
            });
        }

        private void ShowResult(decimal value, bool updateExpression) {
            this.Dispatcher.BeginInvoke(() => {
                System.Numerics.BigInteger interger = (System.Numerics.BigInteger)value;
                interger_textbox.Text = interger.ToString();
                interger &= 0xFFFFFFFFFFFFFFFF;
                ulong uint64 = (ulong)interger;
                hex_textbox.Text = "0x" + Calculator.IntToHex(uint64);
                result_textblock.Cursor = Cursors.IBeam;
                result_textblock.Foreground = Brushes.Blue;
                result_textblock.Text = ((value % 1) == 0) ? interger_textbox.Text : value.ToString();
                if(updateExpression) {
                    expression_textbox.Text = result_textblock.Text;
                    expression_textbox.SelectionStart = expression_textbox.Text.Length;
                }
                RegisterEventEnabled = false;
                reg0.Value = (byte)(((uint64) >> 0) & 0xFF);
                reg1.Value = (byte)(((uint64) >> 8) & 0xFF);
                reg2.Value = (byte)(((uint64) >> 16) & 0xFF);
                reg3.Value = (byte)(((uint64) >> 24) & 0xFF);
                binary_text_0.Text = Calculator.ByteToBIN(reg0.Value);
                binary_text_1.Text = Calculator.ByteToBIN(reg1.Value);
                binary_text_2.Text = Calculator.ByteToBIN(reg2.Value);
                binary_text_3.Text = Calculator.ByteToBIN(reg3.Value);
                RegisterEventEnabled = true;
            });
        }

        private void CalculationHandler() {
            string oldStr = "";
            string newStr = "";
            expression_textbox.Dispatcher.Invoke(() => {
                oldStr = expression_textbox.Text;
            });
            while(true) {
                try {
                    do {
                        this.Dispatcher.Invoke(() => {
                            newStr = expression_textbox.Text;
                        });
                        Thread.Sleep(50);
                    } while(oldStr == newStr);
                    oldStr = newStr;
                    if(oldStr.Trim() == "") {
                        ClrResult();
                        continue;
                    }
                    ShowResult(Calculator.Decimal(oldStr), false);
                }
                catch {
                    try {
                        StringBuilder expression = new StringBuilder(oldStr);
                        StringBuilder result = new StringBuilder();
                        Match match;
                        if((match = Regex.Match(expression.ToString(), @"(\A|\s+)""(\w|\s)+""(\z|\s+)")) != null && match.Value != "") {
                            byte[] bytes = UTF8Encoding.UTF8.GetBytes(expression.ToString().Substring(1, expression.Length - 2));
                            for(int i = 0; i < bytes.Length - 1; i++) {
                                result.Append(bytes[i]);
                                result.Append(", ");
                            }
                            result.Append(bytes[bytes.Length - 1]);
                        }
                        else if((match = Regex.Match(expression.ToString(), @"(\A|\s+)""(\w|\s)+"".ToUpper(\(\))*(\z|\s+)")) != null && match.Value != "") {
                            string temp = expression.ToString();
                            temp = temp.Substring(0, temp.IndexOf('.'));
                            result = new StringBuilder(temp.ToUpper());
                        }
                        else if((match = Regex.Match(expression.ToString(), @"(\A|\s+)""(\w|\s)+"".ToLower(\(\))*(\z|\s+)")) != null && match.Value != "") {
                            string temp = expression.ToString();
                            temp = temp.Substring(0, temp.IndexOf('.'));
                            result = new StringBuilder(temp.ToLower());
                        }
                        if(result.Length > 0) {
                            this.Dispatcher.BeginInvoke(() => {
                                result_textblock.Cursor = Cursors.IBeam;
                                result_textblock.Foreground = Brushes.Blue;
                                result_textblock.Text = result.ToString();
                            });
                        }
                        else {
                            this.Dispatcher.BeginInvoke(() => {
                                result_textblock.Cursor = Cursors.Arrow;
                                result_textblock.Text = "Invalid expression";
                                result_textblock.Foreground = Brushes.Red;
                            });
                        }
                    }
                    catch {

                    }
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            this.Topmost = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            this.Topmost = false;
        }

        private void expression_textbox_KeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter) {
                try {
                    ShowResult(Calculator.Decimal(expression_textbox.Text), true);
                }
                catch {

                }
            }
        }

        private void result_textblock_SelectionChanged(object sender, RoutedEventArgs e) {
            if(result_textblock.Foreground != Brushes.Blue)
                if(result_textblock.SelectionLength != 0)
                    result_textblock.SelectionLength = 0;
        }
    }
}
