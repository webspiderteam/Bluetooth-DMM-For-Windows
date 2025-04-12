using System;
using System.Diagnostics;
using System.Linq;

namespace BluetoothDLL.Bluetooth
{
    public static partial class Utilities
    {
        private static void BDMDecode(byte[] data)
        {
            var newValue = "";
            var datashift = new byte[] { 65, 33, 115, 85, 256 - 94, 256 - 63, 50, 113, 102, 256 - 86, 59, 256 - 48, 256 - 30, 256 - 88, 51, 20, 32, 26, 256 - 86, 256 - 69 };
            var tmp = "";
            bool[] subValue = { };
            bool[] msubValue = { };
            int i = 0;
            foreach (var binaryval in data)
            {
                tmp = new string(Convert.ToString(binaryval ^ datashift[i], 2).PadLeft(8, '0').ToArray());
                newValue += tmp;
                i++;
            }

            if (oldMessage != newValue || oldMessage == "")
            {
                string[] pre_digits = new string[] { "-", ".", ".", "." };
                string[] digits = new string[] { "", "", "", "" };
                int dev_type = data[2] ^ datashift[2];
                MyGattCDataType = dev_type;
                if (dev_type != 4)
                {
                    for (int n = 0; n < 4; n++)
                    {
                        int fi = (n + 3) * 8;
                        string first = newValue.Substring(fi, 3);
                        int si = ((n + 4) * 8) + 4;
                        string second = newValue.Substring(si, 4);
                        digits[n] = (newValue.Substring(((n + 3) * 8) + 3, 1).Equals("1") ? pre_digits[n] : string.Empty) + ParsedigitBDM(first + second);
                    }
                }
                else
                {
                    pre_digits = new string[] { ".", ".", ".", ".", "" };// (newValue.Substring((13 * 8) + 3, 1).Equals("0") ? ":" : "."), ".", "" };
                    digits = new string[] { "", "", "", "", "" };
                    int digitno = 0;
                    for (int n = 13; n > 8; n--)
                    {

                        int fi = n * 8;
                        string first = newValue.Substring(fi, 3);
                        int si = (n * 8) + 4;
                        string second = newValue.Substring(si, 4);
                        digitno = 13 - n;
                        digits[digitno] = (newValue.Substring((n * 8) + 3, 1).Equals("1") ? pre_digits[n - 9] : string.Empty) + (n != 13 ? ParsedigitBDM(first + second) : ParsedigitBDM(first.Substring(0, 1) + "00") + ParsedigitBDM(second.Substring(0, 2) + "00"));
                    }
                }
                MyGattCData = string.Join("", digits);
                Debug.WriteLine(String.Format("NewVal {0} at {1}", newValue, DateTime.Now.ToString()));

                oldMessage = newValue;

                if (data.Count() == 11)
                {
                    MyGattCDataHold = newValue.Substring(59, 1).Equals("1");
                    MyGattCDataRel = newValue.Substring(30, 1).Equals("1");
                    MyGattCDataACDC = (newValue.Substring(68, 1).Equals("1") ? "AC" : String.Empty) +
                       (newValue.Substring(73, 1).Equals("1") ? "DC" : String.Empty);

                    MyGattCDataSymbol = (newValue.Substring(57, 1).Equals("1") ? "°C" : String.Empty) +
                                        (newValue.Substring(58, 1).Equals("1") ? "°F" : String.Empty) +
                                        (newValue.Substring(74, 1).Equals("1") ? "m" : String.Empty) +
                                        (newValue.Substring(75, 1).Equals("1") ? "V" : String.Empty) +
                                        (newValue.Substring(64, 1).Equals("1") ? "n" : String.Empty) +
                                        (newValue.Substring(65, 1).Equals("1") ? "m" : String.Empty) +
                                        (newValue.Substring(66, 1).Equals("1") ? "µ" : String.Empty) +
                                        (newValue.Substring(67, 1).Equals("1") ? "F" : String.Empty) +
                                        (newValue.Substring(69, 1).Equals("1") ? "%" : String.Empty) +
                                        (newValue.Substring(76, 1).Equals("1") ? "M" : String.Empty) +
                                        (newValue.Substring(77, 1).Equals("1") ? "k" : String.Empty) +
                                        (newValue.Substring(78, 1).Equals("1") ? "Ω" : String.Empty) +
                                        (newValue.Substring(79, 1).Equals("1") ? "Hz" : String.Empty) +
                                        (newValue.Substring(85, 1).Equals("1") ? "µ" : String.Empty) +
                                        (newValue.Substring(84, 1).Equals("1") ? "m" : String.Empty) +
                                        (newValue.Substring(72, 1).Equals("1") ? "A" : String.Empty);
                    MyGattCDataMax = newValue.Substring(71, 1).Equals("1");
                    MyGattCDataMin = newValue.Substring(70, 1).Equals("1");
                    MyGattCDataTrue_RMS = newValue.Substring(68, 1).Equals("1");
                    MyGattCDataAutoRange = newValue.Substring(87, 1).Equals("1");
                    MyGattCDataDiode = newValue.Substring(56, 1).Equals("1");
                    MyGattCDataContinuity = newValue.Substring(28, 1).Equals("1");
                    MyGattCDataBattery = newValue.Substring(31, 1).Equals("1");

                }
                else if (data.Count() == 10)
                {

                    if (dev_type == 2)
                    {
                        subValue = (newValue.Substring(28, 4) +
                                    newValue.Substring(56, 4) +
                                    newValue.Substring(68, 4) +
                                    newValue.Substring(64, 4) +
                                    newValue.Substring(76, 4) +
                                    newValue.Substring(72, 4)).Select(c => c == '1').ToArray();
                        msubValue = ("0000" + "0000").Select(c => c == '1').ToArray();
                    }
                    else if (dev_type == 1)
                    {
                        subValue = (newValue.Substring(28, 4) +
                                    newValue.Substring(72, 4) +
                                    newValue.Substring(56, 4) +
                                    newValue.Substring(68, 4) +
                                    newValue.Substring(64, 4) +
                                    newValue.Substring(76, 4)).Select(c => c == '1').ToArray();
                        msubValue = (newValue.Substring(72, 4) + "0000").Select(c => c == '1').ToArray();
                    }
                    MyGattCDataHold = subValue[2];
                    MyGattCDataHV = Convert.ToBoolean(Convert.ToInt16(subValue[1]) ^ Convert.ToInt16(newValue.Substring(23, 1)));
                    MyGattCDataACDC = (subValue[8] ? "AC" : String.Empty) +
                                        (subValue[9] ? "DC" : String.Empty);

                    MyGattCDataSymbol = (subValue[20] ? "°C" : String.Empty) +
                                          (subValue[21] ? "°F" : String.Empty) +
                                          (subValue[16] ? "M" : String.Empty) +
                                          (subValue[18] ? "k" : String.Empty) +
                                          (subValue[19] ? "Ω" : String.Empty) +
                                          ((subValue[23] && dev_type != 1) ? "%" : String.Empty) +
                                          (subValue[22] ? "Hz" : String.Empty) +
                                          (subValue[11] ? "n" : String.Empty) +
                                          (subValue[12] ? "µ" : String.Empty) +
                                          (subValue[17] ? "m" : String.Empty) +
                                          (subValue[10] ? "V" : String.Empty) +
                                          (subValue[15] ? "F" : String.Empty) +
                                          (subValue[13] ? "A" : String.Empty);
                    MyGattCDataTrue_RMS = subValue[8];
                    MyGattCDataDiode = subValue[14];
                    MyGattCDataContinuity = subValue[0];
                    MyGattCDataBattery = subValue[3];
                    MyGattCDataPeek = msubValue[1];
                    MyGattCDataInRush = msubValue[3];
                }
                else if (data.Count() > 18 && dev_type == 4)
                {
                    subValue = (newValue.Substring(28, 4) +
                                        newValue.Substring(24, 4) +
                                        newValue.Substring(36, 4) +
                                        newValue.Substring(32, 4) +
                                        newValue.Substring(108, 4) +
                                        newValue.Substring(104, 4) +
                                        newValue.Substring(128, 4) +
                                        newValue.Substring(140, 4) +
                                        newValue.Substring(144, 4)).Select(c => c == '1').ToArray();
                    msubValue = ("0000" + "0000").Select(c => c == '1').ToArray();
                    MyGattCDataHold = subValue[8];
                    MyGattCDataACDC = (subValue[21] ? "AC" : String.Empty) +
                                      (subValue[18] ? "DC" : String.Empty);


                    MyGattCDataSymbol = (subValue[27] ? "M" : String.Empty) +
                                          (subValue[26] ? "k" : String.Empty) +
                                          (subValue[25] ? "Ω" : String.Empty) +
                                          //(subValue[23] ? "%" : String.Empty) +
                                          (subValue[24] ? "Hz" : String.Empty) +
                                          (subValue[28] ? "n" : String.Empty) +
                                          (subValue[12] ? "µ1" : String.Empty) +
                                          (subValue[30] ? "µ" : String.Empty) +
                                          (subValue[29] ? "m" : String.Empty) +
                                          (subValue[15] ? "V" : String.Empty) +
                                          (subValue[31] ? "F" : String.Empty) +
                                          (subValue[35] ? "A" : String.Empty);
                    MyGattCDataTrue_RMS = subValue[21];
                    MyGattCDataDiode = subValue[5];
                    MyGattCDataContinuity = subValue[6];
                    MyGattCDataPeek = subValue[9];
                    MyGattCDataMax = subValue[10];
                    MyGattCDataMin = subValue[11];
                    MyGattCDataBattery = subValue[3];
                    MyGattCDataRel = subValue[7];
                    MyGattCDataAutoRange = subValue[1];
                    Debug.WriteLine(String.Format("NewSubVal {0} at {1}", string.Join(", ", subValue), DateTime.Now.ToString()));
                }
            }
        }
        private static string ParsedigitBDM(string digitraw)
        {

            switch (digitraw)
            {
                ///  aaa --> ebagfdc (flipped)
                ///  b c
                ///  ddd
                ///  e f 
                ///  ggg ----> abecdfg (rightorder)

                case "0000000": return " ";
                case "1111110": return "A";
                case "0010011": return "U";
                case "0110101": return "T";
                case "0010111": return "O";
                case "1110101": return "E";
                case "1110100": return "F";
                case "0110001": return "L";
                case "0000100": return "-";
                case "1111011": return "0";
                case "0001010": return "1";
                case "1011101": return "2";
                case "1001111": return "3";
                case "0101110": return "4";
                case "1100111": return "5";
                case "1110111": return "6";
                case "1001010": return "7";
                case "1111111": return "8";
                case "1101111": return "9";
                case "0000": return "";//p66 firstdigit
                case "1100": return "1";//p66 firstdigit
                case "100": return "-";//P66
                case "000": return "";//P66
                default: return "?";
            }
        }
    }
}