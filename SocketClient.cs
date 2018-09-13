using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using wims.kr.co.wims.util;
using wims.kr.co.wims.global;
using System.Configuration;

namespace WIMS_server
{
    public class SocketClient
    {
        public Boolean isReceiv = true;

        public string EQUIPMENT_CODE = "";
        public string USE_SECT = "";
        public string EQUIPMENT_IP = "";
        public int EQUIPMENT_PORT = 11000;

        public int iReceiveBufferSize = 11;
        public int iSendBufferSize = 3;

        public bool bLoopMore = true;

        public void log(string strMessage)
        {
            LogHelper.log(strMessage, EQUIPMENT_PORT + "");
        }

        public void log(System.Exception ex)
        {
            LogHelper.log(ex, EQUIPMENT_PORT + "");
        }


        public void Client()
        {
            log("WIMS SERVER Thread Start[ Equip :"+ EQUIPMENT_CODE + " ]!!!");

            IPHostEntry ipHostInfo = Dns.Resolve(EQUIPMENT_IP);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, EQUIPMENT_PORT);

            // create tcp/ip socket
            log("Creating WIMS SERVER Thread Socket [ Equip :" + EQUIPMENT_CODE + " ] !!!");
            Socket socketClient = null;
            log("Created WIMS SERVER Thread Socket [ Equip :" + EQUIPMENT_CODE + " ] !!!");

            byte[] arrReceiveBytes = new byte[iReceiveBufferSize];
            byte[] arrSendeBytes = new byte[iSendBufferSize] ;

            while (bLoopMore)
            {
                log("--------------------------BEGIN COMMUNICATION LOOP [ Equip :" + EQUIPMENT_CODE + " ] --------------------------");
                try
                {
                    if (socketClient == null)
                    {
                        socketClient = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        socketClient.SendTimeout = 10 * 1000;
                        socketClient.ReceiveTimeout = 10 * 1000;
                    }

                    log("CONNECTING  [ Equip :" + EQUIPMENT_CODE + " ]  : " + remoteEP.ToString());
                    socketClient.Connect(remoteEP);
                    socketClient.NoDelay = true;
                    log("CLIENT Socket CONNECTED TO  [ Equip :" + EQUIPMENT_CODE + " ]  : " + socketClient.RemoteEndPoint.ToString());

                    isReceiv = Global.getConnectState(EQUIPMENT_CODE);

                    while (isReceiv)
                    {
                        isReceiv = Global.getConnectState(EQUIPMENT_CODE);

                        log("*******************COMMUNICATING WITH AC DEVICE (START [ Equip :" + EQUIPMENT_CODE + " ]  )*******************");
                        //수신data 대한 응답을 보낸다.----------------------------------------------------

                        log("Client Socket Receiving [ Equip :" + EQUIPMENT_CODE + " ]");
                        int iReceivedLength = socketClient.Receive(arrReceiveBytes);

                        log("Client Socket Received Length [ Equip :" + EQUIPMENT_CODE + " ]: " + iReceivedLength);
                        log("Client Socket Received Bytes [ Equip :" + EQUIPMENT_CODE + " ]" + Encoding.ASCII.GetString(arrReceiveBytes));

                        //수신data 대한 응답을 보낸다.----------------------------------------------------
                        arrSendeBytes = new byte[] { 0x0002  // STX : 1byte 02H
                                                     , 0x0006  // ACK : 1byte 06H / NAK : 1byte 15H
                                                     , 0x0003  // ETX : 1byte 03H
                                                     };

                        //수신data 입력한다.----------------------------------------------------
                        log("DataBase BEFORE INSERT [ Equip :" + EQUIPMENT_CODE + " ] ... ");
                        ReceiveDAO recv_insert = new ReceiveDAO(ConfigurationManager.ConnectionStrings["WIMS_server.Properties.Settings.wimsConnectionString"].ToString());

                        Dictionary<string, string> getResult = recv_insert.actionInsert(EQUIPMENT_CODE, arrReceiveBytes);

                        if (getResult["ERR"] == "FALSE" && iReceivedLength >= 11)
                        {
                            log("Client Socket send msg DataBase INSERT SUCCESS [ Equip :" + EQUIPMENT_CODE + " ] ... ");

                            // 수신 입력량을 추가한다.
                            Global.setRecvCnt(EQUIPMENT_CODE, 1, "add");
                        }
                        else
                        {
                            if(iReceivedLength != 11)
                                log("Client Socket Length InCoreect ! Recvice byte cnt  [ Equip :" + EQUIPMENT_CODE + " ]: " + iReceivedLength);
                            else if(getResult["ERR"] != "FALSE")
                                log("입력에 실패하였습니다.(DB INSERT ERROR) [ Equip :" + EQUIPMENT_CODE + " ] : " + getResult["MSG"]);
                            arrSendeBytes[1] = 0x0015;

                        }

                        log("Client Socket Sending Response [ Equip :" + EQUIPMENT_CODE + " ] : " + Encoding.UTF8.GetString(arrSendeBytes));
                        int iSendedLength = socketClient.Send(arrSendeBytes);
                        log("Client Socket Sended Response Length [ Equip :" + EQUIPMENT_CODE + " ] : " + iSendedLength);

                        log("*******************COMMUNICATING WITH AC DEVICE (END [ Equip :" + EQUIPMENT_CODE + " ] )*******************");

                        //스크롤 너무 빨리 되는 것을 방지 하기 위해서 강제뢰 쉼 
                        //실제 운영시는 필요하지 않을 것으로 예상함 
                        //System.Threading.Thread.Sleep(5 * 1000);
                    }
                    socketClient.Close();
                    log("Closed Client Socket [ Equip :" + EQUIPMENT_CODE + " ]");
                }
                catch (SocketException se)
                {
                    log("상대방과 통신할 수 없습니다. ( [ Equip :" + EQUIPMENT_CODE + " ] 상대방 HOST 준비 되지 않음 : Socket Exception)" + se.ToString());

                    //통신에러를 건수를 표기한다.
                    Global.setConErrCnt(EQUIPMENT_CODE, 1, "add");
                }
                catch(Exception e)
                {
                    log(e);

                    //통신에러를 건수를 표기한다.
                    Global.setConErrCnt(EQUIPMENT_CODE, 1, "add");
                }
                finally
                {
                    if(socketClient != null  )
                    {
                        try { socketClient.Close();}catch (Exception e){}
                        socketClient = null;

                    }

                    //스크롤 너무 빨리 되는 것을 방지 하기 위해서 강제뢰 쉼 
                    //실제 운영시는 필요하지 않을 것으로 예상함 
                    System.Threading.Thread.Sleep(1 * 1000);
                }

                log("--------------------------BEGIN COMMUNICATION LOOP  [ Equip :" + EQUIPMENT_CODE + " ]--------------------------");
            }
            log("WIMS SERVER Thread END [ Equip :" + EQUIPMENT_CODE + " ] !!!");
        }

        public void ClientStart()
        {
            Client();
        }
    }

}