using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.ComponentModel;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Events;

namespace MiniDeck.Services
{
    public class ActionService
    {
        public ActionService()
        {
        }

        public async void ExecuteKeyPress(string keySequence)
        {
            if (string.IsNullOrWhiteSpace(keySequence)) return;

            try
            {
                // キーシーケンスをパースしてキーコードに変換
                var keys = ParseKeySequence(keySequence);
                
                if (keys.Count == 0)
                {
                    MessageBox.Show($"キーシーケンス '{keySequence}' を認識できません。", 
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // WindowsInputを使用してキーを送信
                var events = Simulate.Events();
                events.ClickChord(keys.ToArray());
                await events.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"キー送信エラー: {keySequence}\n{ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<KeyCode> ParseKeySequence(string keySequence)
        {
            var keys = new List<KeyCode>();
            
            // "Ctrl+Shift+R" のような形式をパース
            var parts = keySequence.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var keyCode = ConvertToKeyCode(trimmed);
                if (keyCode.HasValue)
                {
                    keys.Add(keyCode.Value);
                }
            }
            
            return keys;
        }

        private KeyCode? ConvertToKeyCode(string key)
        {
            // 大文字小文字を無視
            var upperKey = key.ToUpperInvariant();
            
            // モディファイアキー
            switch (upperKey)
            {
                case "CTRL":
                case "CONTROL":
                    return KeyCode.LControl;
                case "ALT":
                    return KeyCode.LAlt;
                case "SHIFT":
                    return KeyCode.LShift;
                case "WIN":
                case "WINDOWS":
                    return KeyCode.LWin;
                    
                // ファンクションキー
                case "F1": return KeyCode.F1;
                case "F2": return KeyCode.F2;
                case "F3": return KeyCode.F3;
                case "F4": return KeyCode.F4;
                case "F5": return KeyCode.F5;
                case "F6": return KeyCode.F6;
                case "F7": return KeyCode.F7;
                case "F8": return KeyCode.F8;
                case "F9": return KeyCode.F9;
                case "F10": return KeyCode.F10;
                case "F11": return KeyCode.F11;
                case "F12": return KeyCode.F12;
                
                // 特殊キー
                case "ENTER":
                case "RETURN":
                    return KeyCode.Return;
                case "ESC":
                case "ESCAPE":
                    return KeyCode.Escape;
                case "TAB":
                    return KeyCode.Tab;
                case "SPACE":
                case "SPACEBAR":
                    return KeyCode.Space;
                case "BACKSPACE":
                case "BACK":
                    return KeyCode.Backspace;
                case "DELETE":
                case "DEL":
                    return KeyCode.Delete;
                case "INSERT":
                case "INS":
                    return KeyCode.Insert;
                case "HOME":
                    return KeyCode.Home;
                case "END":
                    return KeyCode.End;
                case "PAGEUP":
                case "PGUP":
                    return KeyCode.Prior;
                case "PAGEDOWN":
                case "PGDN":
                    return KeyCode.Next;
                case "UP":
                    return KeyCode.Up;
                case "DOWN":
                    return KeyCode.Down;
                case "LEFT":
                    return KeyCode.Left;
                case "RIGHT":
                    return KeyCode.Right;
                case "PRINTSCREEN":
                case "PRTSC":
                    return KeyCode.Snapshot;
                case "PAUSE":
                    return KeyCode.Pause;
                case "CAPSLOCK":
                    return KeyCode.CapsLock;
                case "NUMLOCK":
                    return KeyCode.NumLock;
                case "SCROLLLOCK":
                    return KeyCode.Scroll;
                    
                // アルファベットキー (A-Z)
                case "A": return KeyCode.A;
                case "B": return KeyCode.B;
                case "C": return KeyCode.C;
                case "D": return KeyCode.D;
                case "E": return KeyCode.E;
                case "F": return KeyCode.F;
                case "G": return KeyCode.G;
                case "H": return KeyCode.H;
                case "I": return KeyCode.I;
                case "J": return KeyCode.J;
                case "K": return KeyCode.K;
                case "L": return KeyCode.L;
                case "M": return KeyCode.M;
                case "N": return KeyCode.N;
                case "O": return KeyCode.O;
                case "P": return KeyCode.P;
                case "Q": return KeyCode.Q;
                case "R": return KeyCode.R;
                case "S": return KeyCode.S;
                case "T": return KeyCode.T;
                case "U": return KeyCode.U;
                case "V": return KeyCode.V;
                case "W": return KeyCode.W;
                case "X": return KeyCode.X;
                case "Y": return KeyCode.Y;
                case "Z": return KeyCode.Z;
                
                // 数字キー (0-9)
                case "0": return KeyCode.D0;
                case "1": return KeyCode.D1;
                case "2": return KeyCode.D2;
                case "3": return KeyCode.D3;
                case "4": return KeyCode.D4;
                case "5": return KeyCode.D5;
                case "6": return KeyCode.D6;
                case "7": return KeyCode.D7;
                case "8": return KeyCode.D8;
                case "9": return KeyCode.D9;
                
                // テンキー
                case "NUMPAD0": return KeyCode.NumPad0;
                case "NUMPAD1": return KeyCode.NumPad1;
                case "NUMPAD2": return KeyCode.NumPad2;
                case "NUMPAD3": return KeyCode.NumPad3;
                case "NUMPAD4": return KeyCode.NumPad4;
                case "NUMPAD5": return KeyCode.NumPad5;
                case "NUMPAD6": return KeyCode.NumPad6;
                case "NUMPAD7": return KeyCode.NumPad7;
                case "NUMPAD8": return KeyCode.NumPad8;
                case "NUMPAD9": return KeyCode.NumPad9;
                case "MULTIPLY": return KeyCode.Multiply;
                case "ADD": return KeyCode.Add;
                case "SUBTRACT": return KeyCode.Subtract;
                case "DIVIDE": return KeyCode.Divide;
                case "DECIMAL": return KeyCode.Decimal;
                
                default:
                    // 1文字のキーの場合
                    if (upperKey.Length == 1)
                    {
                        char c = upperKey[0];
                        if (c >= 'A' && c <= 'Z')
                        {
                            return (KeyCode)c;
                        }
                    }
                    return null;
            }
        }

        public void LaunchApplication(string path, string arguments)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(path);
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    startInfo.Arguments = arguments;
                }
                Process.Start(startInfo);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"アプリケーションの起動に失敗しました: {path}\n{ex.Message}", 
                    "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"予期しないエラー: {ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}