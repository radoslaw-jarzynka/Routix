using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuickGraph;
using AddressLibrary;
using System.IO;
using QuickGraph.Glee;
using Microsoft.Glee.Drawing;

namespace Routix {

    public partial class Routix : Form {

        delegate void SetTextCallback(string text);
        delegate void SetGraphCallback(Microsoft.Glee.Drawing.Graph g);

        private const bool isDebug = true;

        private Address myAddr;
        
        //dane chmury
        private IPAddress cloudAddress;        //Adres na którym chmura nasłuchuje
        private Int32 cloudPort;           //port chmury
        private IPEndPoint cloudEndPoint;
        private Socket cloudSocket;

        private Thread receiveThread;     //wątek służący do odbierania połączeń
        private Thread sendThread;        // analogicznie - do wysyłania

        private Queue _whatToSendQueue;
        private Queue whatToSendQueue;

        //strumienie
        private NetworkStream networkStream;

        private StreamReader reader;
        private StreamWriter writer;

        public bool isConnectedToCloud { get; private set; } // czy połączony z chmurą?

        //biblioteka z tymi grafami jest chujowa i nie da się przypisać słownikowi w niej zawartemu odpowiedniego comparera
        //dlatego graf będzie zawierać adresy w postaci odpowiadających im stringów :<
        private AdjacencyGraph<String, Edge<String>> networkGraph;
        /// <summary>
        /// konstruktor
        /// </summary>
        public Routix() {
            isConnectedToCloud = false;
            InitializeComponent();
            networkGraph = new AdjacencyGraph<String, Edge<String>>();
            _whatToSendQueue = new Queue();
            //synchroniczny wrapper dla kolejki
            whatToSendQueue = Queue.Synchronized(_whatToSendQueue);
        }

        #region connections
        /// <summary>
        /// metoda wywołana po wciśnięciu "połącz z chmurą"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void conToCloudButton_Click(object sender, EventArgs e) {
            if (!isConnectedToCloud) {
                if (setAddress()) {
                    if (IPAddress.TryParse(cloudIPTextBox.Text, out cloudAddress)) {
                        SetText("IP ustawiono jako " + cloudAddress.ToString());
                    } else {
                        SetText("Błąd podczas ustawiania IP chmury (zły format?)");
                    }
                    if (Int32.TryParse(cloudPortTextBox.Text, out cloudPort)) {
                        SetText("Port chmury ustawiony jako " + cloudPort.ToString());
                    } else {
                        SetText("Błąd podczas ustawiania portu chmury (zły format?)");
                    }

                    cloudSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    cloudEndPoint = new IPEndPoint(cloudAddress, cloudPort);
                    try {
                        cloudSocket.Connect(cloudEndPoint);
                        isConnectedToCloud = true;
                        networkStream = new NetworkStream(cloudSocket);
                        writer = new StreamWriter(networkStream);
                        reader = new StreamReader(networkStream);
                        sendButton.Enabled = true;
                        whatToSendQueue.Enqueue("HELLO " + myAddr);
                        receiveThread = new Thread(this.receiver);
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                        sendThread = new Thread(this.sender);
                        sendThread.IsBackground = true;
                        sendThread.Start();
                        conToCloudButton.Text = "Rozłącz";
                        SetText("Połączono!");
                    } catch (SocketException) {
                        isConnectedToCloud = false;
                        SetText("Błąd podczas łączenia się z chmurą");
                        SetText("Złe IP lub port? Chmura nie działa?");
                    }
                } else {
                    SetText("Wprowadź numery sieci i podsieci");
                }
            } else {
                isConnectedToCloud = false;
                sendButton.Enabled = false;
                conToCloudButton.Text = "Połącz";
                SetText("Rozłączono!");
                if (cloudSocket != null) cloudSocket.Close();
            }
        }
        #endregion

        #region threads
        /// <summary>
        /// wątek odbierający wiadomości z chmury
        /// </summary>
        public void receiver() {
            String _msg;
            while (isConnectedToCloud) {
                _msg = reader.ReadLine();
                if (isDebug) SetText("Odczytano: " + _msg);
                String[] _msgArray = _msg.Split(new char[] {':'}, 2);
                Address _senderAddr;
                if (Address.TryParse(_msgArray[0], out _senderAddr)) {
                    if (_senderAddr.host == 0) {
                    #region FROM ANOTHER RC

                    #endregion
                    }
                    else if (_senderAddr.host == 1) {
                    #region FROM CC
                        
                    #endregion
                    }
                    else {
                    #region FROM LRM
                        String[] _LRMmsg = _msgArray[1].Split(new char[] { ' ' });
                        //gdy logowanie się LRM
                        if (_LRMmsg[0] == "HELLO") {
                            Address _addr;
                            if (Address.TryParse(_LRMmsg[1], out _addr)) {
                                if (networkGraph.ContainsVertex(_addr.ToString())) {
                                    whatToSendQueue.Enqueue(_senderAddr.ToString() + ":ADDR_TAKEN");
                                } else {
                                    networkGraph.AddVertex(_addr.ToString());
                                    if (isDebug) SetText("Dodano węzeł grafu");
                                    whatToSendQueue.Enqueue(_addr.ToString() + ":REQ_TOPOLOGY");
                                }
                            }
                        }
                        if (_LRMmsg[0] == "TOPOLOGY") {
                            String[] _neighbors = new String[_LRMmsg.Length - 1];
                            for (int i = 1; i < _LRMmsg.Length; i++) {
                                _neighbors[i - 1] = _LRMmsg[i];
                            }
                            foreach (String str in _neighbors) {
                                Address _destAddr;
                                Edge<string> x; //tylko temporary
                                if (Address.TryParse(str, out _destAddr)) {
                                    //jeśli jest już taka ścieżka nic nie rób
                                    if (networkGraph.TryGetEdge(_senderAddr.ToString(), _destAddr.ToString(), out x)) {
                                    }
                                        //jeśli nie ma
                                    else {
                                        //jeśli nie ma w węzłach grafu węzła z topologii - dodaj go
                                        if (!networkGraph.Vertices.Contains(_destAddr.ToString())) networkGraph.AddVertex(_destAddr.ToString());
                                        //dodaj ścieżkę
                                        networkGraph.AddEdge(new Edge<String>(_senderAddr.ToString(), _destAddr.ToString()));
                                        if (isDebug) SetText("Dodano ścieżkę z " + _senderAddr.ToString() + " do " + _destAddr.ToString());
                                        //rysuj graf
                                        fillGraph();
                                    }
                                }
                            }
                        }
                    #endregion
                    }
                }
            }
        }
        /// <summary>
        /// wątek wysyłający wiadomości do chmury
        /// </summary>
        public void sender() {
            while(isConnectedToCloud) {
                //jeśli coś jest w kolejce - zdejmij i wyślij
                if (whatToSendQueue.Count != 0) {
                    String _msg = (String)whatToSendQueue.Dequeue();
                    writer.WriteLine(_msg);
                    writer.Flush();
                    if (isDebug) SetText("Wysłano: " + _msg);
                }
            }    
        }
        #endregion

        #region logging & address setup
        /// <summary>
        /// metoa ustalająca adres RC
        /// </summary>
        /// <returns>czy się udało czy nie</returns>
        public bool setAddress() {
            int _netNum;
            int _subnetNum;
            if (int.TryParse(networkNumberTextBox.Text, out _netNum))
                if (int.TryParse(subnetTextBox.Text, out _subnetNum)) {
                     myAddr = new Address(_netNum, _subnetNum, 0);
                    return true;
                }
                else return false;
            else return false;
        }
        /// <summary>
        /// wstawienie tekstu do logu
        /// </summary>
        /// <param name="text">tekst, który metoda ma wstawić</param>
        public void SetText(string text) {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (this.log.InvokeRequired) {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            } else {
                try {
                    this.log.AppendText(text+"\n");
                } catch { }
            }
        }
        #endregion

        #region sending messages
        /// <summary>
        /// metoda wywołana po wciśnięciu "wyślij"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendButton_Click(object sender, EventArgs e) {
            whatToSendQueue.Enqueue(sendTextBox.Text);
            sendTextBox.Clear();
        }
        /// <summary>
        /// obsługa wciśnięcia klawisza ENTER
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendTextBox_KeyPress(object sender, KeyPressEventArgs e) {
            if (sendButton.Enabled && e.KeyChar.Equals((char)Keys.Enter)) sendButton_Click(sender, e);
        }
        #endregion
        #region graph drawing
        /// <summary>
        /// rysowanie grafu w formsie
        /// </summary>
        private void fillGraph() {

            IVertexAndEdgeListGraph<string, Edge<string>> g = networkGraph;
            var populator = GleeGraphExtensions.CreateGleePopulator<String, Edge<String>>(g);
            populator.Compute();
            Microsoft.Glee.Drawing.Graph graph = populator.GleeGraph;
            if (this.gViewer.InvokeRequired) {
                SetGraphCallback d = new SetGraphCallback(SetGraph);
                this.Invoke(d, new object[] { graph });
            } else {
                try {
                    gViewer.Graph = graph;
                } catch { }
            }
        }
        /// <summary>
        /// metoda potrzebna do tego, by zmiana grafu na ekranie była thread-safe
        /// </summary>
        /// <param name="graph">graf do wrzucenia na ekran</param>
        private void SetGraph(Microsoft.Glee.Drawing.Graph graph) {
            gViewer.Graph = graph;
        }
        #endregion
    }
}
