using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using wims.kr.co.wims.util;

namespace WIMS_DeviceEmulator
{
    public class SocketListener
    {
        public Boolean bSend = true;
        public Boolean bLoopMore = true;
        public int iSendBufferSize = 11;
        public int iReceiveBufferSize = 3; 

        public int port = 11000;

        public void log(string strMessage)
        {
            LogHelper.log(strMessage, port + "");
        }

        public void log(System.Exception ex)
        {
            LogHelper.log(ex, port + "");
        }

        public void Listening()
        {
            log("SynchronusSoketLinstner port[" + port + "] start !!!");

            byte[] arrSendBytes = new Byte[iSendBufferSize];
            byte[] arrReceiveBytes = new Byte[iReceiveBufferSize];

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            log("LISTENER Socket port[" + port + "] create ");
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.SendTimeout = 10*1000;
            listener.ReceiveTimeout = 10*1000;

            listener.Bind(localEndPoint);
            log("LISTENER Socket port[" + port + "] Bind ...");
            listener.Listen(2);
            log("LISTENER Socket port[" + port + "] Listen ...");

            //Socket To Client 
            Socket handler = null;

            while (true)
            {
                try
                {
                    log("--------------------------AC DEVICE EMULATION COMMUNICATION BEGIN port[" + port + "] --------------------------");
                    log("LISTENER Socket BEFORE ACCEPT port[" + port + "] ");
                    handler = listener.Accept();
                    handler.NoDelay = true;
                    log("LISTENER Socket Accepted  port[" + port + "]... ");

                    string strIsOkBefore = "OK";

                    while (true)
                    {
                        log("##############################AC DEVICE START SEND port[" + port + "] ############################## ");

                        if (strIsOkBefore == "OK")
                        {
                            //read next 
                            arrSendBytes = new byte[] { 0x0002                                          // STX : 1byte 20H
                                                        , 0x0032, 0x0031                                  // 품번 : 2byte 
                                                        , 0x0050                                          // 등급 U:55H / P:50H / O:4FH
                                                        , 0x0031, 0x0032, 0x0033, 0x002E, 0x0031, 0x0032  // 판정중량 6byte 소수점:2EH 1~9 : 31H~39H
                                                        , 0x0003                                          // ETX:03H
                                                        };
                            Random r = new Random((int) DateTime.Now.Ticks);
                            //50 Kg 정상 / 40Kg Under/ 60Kg Over/ 그 외 Pass 
                            double dblWeight = 50 + (r.NextDouble() - 0.5)  * 25;
                            if (dblWeight < 40)
                                arrSendBytes[3] = 0x55; 
                            else if (dblWeight > 60)
                                arrSendBytes[3] = 0x4F;
                            else
                                arrSendBytes[3] = 0x50;

                            string fmtWeight = dblWeight.ToString("00.000");
                            arrSendBytes[4] = (byte)fmtWeight[0];
                            arrSendBytes[5] = (byte)fmtWeight[1];
                            arrSendBytes[6] = (byte)fmtWeight[2];
                            arrSendBytes[7] = (byte)fmtWeight[3];
                            arrSendBytes[8] = (byte)fmtWeight[4];
                            arrSendBytes[9] = (byte)fmtWeight[5];

                            log("Setted LISTENER Socket Send ByteArray port[" + port + "] ");
                        }
                        else
                            log("Previous stat NAK, using old arrSendeBytes port[" + port + "]");

                        int iSendedLength = handler.Send(arrSendBytes);
                        log("LISTENER Socket port[" + port + "] Sent Length : " + iSendedLength);
                        log("LISTENER Socket port[" + port + "] Sent Bytes(String) : " + Encoding.ASCII.GetString(arrSendBytes));
                        log("LISTENER Socket port[" + port + "] Sent Bytes(HEX) : " + ByteHelper.ByteArrayToHexString(arrSendBytes, ","));

                        int iReceivedLength = handler.Receive(arrReceiveBytes);
                        if(iReceivedLength == 0)
                        {
                            log("Socket Disconnected . Reconnect Procedure starts");
                            break; 
                        }

                        log("LISTENER Socket port[" + port + "] Received Length : " + iReceivedLength);
                        log("LISTENER Socket port[" + port + "] Received Bytes(String) : " + Encoding.ASCII.GetString(arrReceiveBytes));
                        log("LISTENER Socket port[" + port + "] Received Bytes(HEX) : " + ByteHelper.ByteArrayToHexString(arrReceiveBytes, ","));

                        // STX : 1byte 02H | ACK : 1byte 06H / NAK : 1byte 15H | ETX : 1byte 03H
                        if (arrReceiveBytes[1] == 0x0006)
                        {
                            log("LISTENER Socket port[" + port + "] receive msg : ACK");
                            strIsOkBefore = "OK";
                        }
                        else
                        {
                            log("LISTENER Socket port[" + port + "] receive msg : NAK");
                            strIsOkBefore = "NG";
                        }

                        log("##############################AC DEVICE START END port[" + port + "] ############################## ");
                        //스크롤 너무 빨리 되는 것을 방지 하기 위해서 강제뢰 쉼 
                        //실제 운영시는 필요하지 않을 것으로 예상함 
                        Thread.Sleep(30 * 1000); //실제와 비슷 하도록 30초 쉬고 전송 
                    }
                }
                catch (SocketException se)
                {
                    if(se.ErrorCode == 0x00002746)
                    {
                        log("Socket Close : " + se.ToString() );
                    }
                    else
                        log("상대방과 통신할 수 없습니다. (상대방 HOST 와의 연결 종료 : Client Disconnect : "+se.ToString()+")");
                }
                catch (Exception e)
                {
                    log(e);
                }
                finally
                {
                    if (handler != null)
                    {
                        try{ handler.Close();}catch (Exception e){}
                        handler = null; 
                    }

                }
                log("--------------------------AC DEVICE EMULATION COMMUNICATION ENDS HERE port[" + port + "]--------------------------");
            }
        }

        public void Start()
        {
            Listening();
        }
    }
}