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
using QuickGraph.Algorithms;
using AddressLibrary;
using Packet;
using System.IO;
using QuickGraph.Glee;
using Microsoft.Glee.Drawing;
using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters.Binary;

namespace Routix {

    public partial class Routix : Form {

        delegate void SetTextCallback(string text);
        delegate void SetGraphCallback(Microsoft.Glee.Drawing.Graph g);
        delegate void SetButtonCallback(bool enable);

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

        private bool blockSending;
        private bool firstRun;
        private List<bool> sendRoute;
        int exceptionCount;
        //strumienie
        private NetworkStream networkStream;

        //private StreamReader reader;
        //private StreamWriter writer;

        public bool isConnectedToCloud { get; private set; } // czy połączony z chmurą?

        private List<List<string>> nodesInPath; //lista węzłów w ścieżce, wykorzystywane do tego by program pamiętał o tym jaka ścieżka jest wyznaczana, które LRMY są odpytywane o zasoby i mógł ją przekazać do CC
        private List<List<string>> _nodesInPath;
        private int numberOfRoutes;
        private string nodeSource;
        private string nodeTarget;
        //słownik <mójhost, podsieć z niego osiągalna>
        private Dictionary<Address, List<String>> availableSubnetworks;
        private List<int> availableSubnetworksViaMe;
        //lista <mójLRM, LRM w innej podsieci>
        private List<KeyValuePair<Address, Address>> subnetConnections;

        //private ConcurrentBag<String> nodesInPathBag;

        //biblioteka z tymi grafami jest chujowa i nie da się przypisać słownikowi w niej zawartemu odpowiedniego comparera
        //dlatego graf będzie zawierać adresy w postaci odpowiadających im stringów :<
        private AdjacencyGraph<String, Edge<String>> networkGraph;
        /// <summary>
        /// konstruktor
        /// </summary>
        public Routix() {
            sendRoute = new List<bool>();
            for (int i = 0; i < 100; i++ ) sendRoute.Add(true);
            numberOfRoutes = 0;
            firstRun = true;
            isConnectedToCloud = false;
            blockSending = false;
            InitializeComponent();
            networkGraph = new AdjacencyGraph<String, Edge<String>>();
            nodesInPath = new List<List<string>>();
            _nodesInPath = new List<List<string>>();
            _whatToSendQueue = new Queue();
            availableSubnetworks = new Dictionary<Address, List<String>>(new AddressComparer());
            subnetConnections = new List<KeyValuePair<Address, Address>>();
            availableSubnetworksViaMe = new List<int>();
            exceptionCount = 0;
            //_nodesInPathQueue = new Queue();
            //synchroniczny wrapper dla kolejki
            whatToSendQueue = Queue.Synchronized(_whatToSendQueue);
            //ticks = 0;
            //nodesInPathQueue = Queue.Synchronized(_nodesInPathQueue);
        }

        #region connections and buttons
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
                        //writer = new StreamWriter(networkStream);
                        //reader = new StreamReader(networkStream);
                        List<String> _welcArr = new List<String>();
                        _welcArr.Add("HELLO");
                        SPacket welcomePacket = new SPacket(myAddr.ToString(), new Address(0 ,0, 0).ToString() , _welcArr);
                        whatToSendQueue.Enqueue(welcomePacket);
                        //whatToSendQueue.Enqueue("HELLO " + myAddr);
                        receiveThread = new Thread(this.receiver);
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                        sendThread = new Thread(this.sender);
                        sendThread.IsBackground = true;
                        sendThread.Start();
                        conToCloudButton.Text = "Rozłącz";
                        SetText("Połączono!");
                        exceptionCount = 0;
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
                conToCloudButton.Text = "Połącz";
                SetText("Rozłączono!");
                if (cloudSocket != null) cloudSocket.Close();
            }
        }

        /// <summary>
        /// gdy wciśnięty zostanie przycik ustalający topologię między węzłami
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendTopology_Click(object sender, EventArgs e) {
            sendTopology();
        }

        /// <summary>
        /// wysłanie topologii do innych RC
        /// </summary>
        private void sendTopology() {
            /*
            List<String> subnets = new List<String>();
            List<int> subnetsNumbers = new List<int>();
            subnets = availableSubnetworks.Values.SelectMany(x => x).ToList();
            foreach (String str in subnets) {
                String[] _str = str.Split('.');
                int _subnetNumber = int.Parse(_str[1]);
                if (!subnetsNumbers.Contains(_subnetNumber)) subnetsNumbers.Add(_subnetNumber);
            }
            foreach (int subnetNumber in subnetsNumbers) {
                List<String> paramsList = subnetsNumbers.ConvertAll<string>(x => x.ToString());
                paramsList.Insert(0, "TOPOLOGY");
                SPacket _pck = new SPacket(myAddr.ToString(), new Address(myAddr.network, subnetNumber, 0).ToString(), paramsList);
                whatToSendQueue.Enqueue(_pck);
            }
             */
            List<String> msg = new List<String>();
            msg.Add("TOPOLOGY");
            foreach (int subnet in availableSubnetworksViaMe) {
                msg.Add(subnet.ToString());
            }
            msg.Add("VIA");
            msg.Add(string.Empty + myAddr.subnet);
            foreach (int subnetNumber in availableSubnetworksViaMe) {
                SPacket _pck = new SPacket(myAddr.ToString(), new Address(myAddr.network, subnetNumber, 0).ToString(), msg);
                whatToSendQueue.Enqueue(_pck);
            }
        }
        #endregion

        #region sending & receiving
        /// <summary>
        /// wątek odbierający wiadomości z chmury
        /// </summary>
        public void receiver() {
            while (isConnectedToCloud) {
                BinaryFormatter bf = new BinaryFormatter();
                try {
                    SPacket receivedPacket = (Packet.SPacket)bf.Deserialize(networkStream);
                    //_msg = reader.ReadLine();
                    if (isDebug) SetText("Odczytano:\n" + receivedPacket.ToString());
                    List<String> _msgList = receivedPacket.getParames();
                    Address _senderAddr;
                    if (Address.TryParse(receivedPacket.getSrc(), out _senderAddr)) {
                        if (_senderAddr.host == 0) {
                            #region FROM ANOTHER RC
                            if (_msgList[0] == "TOPOLOGY") {
                                try {
                                    _msgList.RemoveAt(0);
                                    String[] _RCmsg = _msgList.ToArray();
                                    int realSenderSubnet = int.Parse(_RCmsg[_RCmsg.Length - 1]);
                                    int srcSubnetNumber = int.Parse(_RCmsg[_RCmsg.Length - 1]); //ostatnia część wiadomości to numer podsieci-źródła wiadomości
                                    int[] subn = new int[_RCmsg.Length - 2];
                                    for (int i = 0; i < _RCmsg.Length - 2; i++) { //tutaj -2 by ominąć elementy "VIA" i numer podsieci-źródła wiadomości
                                        subn[i] = int.Parse(_RCmsg[i]);
                                    }
                                    foreach (int subnet in subn) {
                                        string _originSubnetAddr = _senderAddr.network + "." + realSenderSubnet + ".*";
                                        string _subnetAddr = myAddr.network + "." + subnet + ".*";
                                        if (subnet == myAddr.subnet) {
                                            //gdy przyszlo od podsieci bezposrednio z nami polaczonej
                                            foreach (Address myHost in availableSubnetworks.Keys) {
                                                List<string> temp;
                                                availableSubnetworks.TryGetValue(myHost, out temp);
                                                foreach (string addr in temp) {
                                                    if (addr == _senderAddr.network + "." + _senderAddr.subnet + ".*") {
                                                        if (!networkGraph.ContainsEdge(_originSubnetAddr, myHost.ToString())) {
                                                            networkGraph.AddEdge(new Edge<string>(_originSubnetAddr, myHost.ToString()));
                                                        }
                                                    }
                                                }
                                            }
                                        } else {
                                            if (!networkGraph.ContainsVertex(_subnetAddr)) {
                                                //gdy mamy już taki wezel grafu - nie rób nic
                                                networkGraph.AddVertex(_subnetAddr);
                                                SetText("Dodaję nowo odkrytą sieć " + _subnetAddr);
                                            }
                                            if (!networkGraph.ContainsEdge(_originSubnetAddr, _subnetAddr)) {
                                                //gdy jest już taka krawędź - nic nie rób
                                                networkGraph.AddEdge(new Edge<string>(_originSubnetAddr, _subnetAddr));
                                                SetText("Dodaję ścieżkę z " + _originSubnetAddr + " do " + _subnetAddr);
                                            }
                                        }


                                        if (!blockSending) sendTopology();

                                        foreach (int _subne in availableSubnetworksViaMe) {
                                            List<string> paramList = receivedPacket.getParames();
                                            if (paramList[0] != "TOPOLOGY") {
                                                paramList.Insert(0, "TOPOLOGY");
                                            }
                                            Address destAddr = new Address(myAddr.network , _subne , 0);
                                            SPacket pck = new SPacket(myAddr.ToString(), destAddr.ToString(), paramList);
                                            if (!blockSending) whatToSendQueue.Enqueue(pck);
                                        }
                                        fillGraph();
                                        ChangeButton(false);
                                    }
                                } catch (Exception e) {
                                    //SetText("Wyjątek przy ustalaniu topologii sieci, do poprawy :<\n" + e.Message);
                                }
                            }
                            #endregion
                        } else if (_senderAddr.host == 1) {
                            #region FROM CC
                            //_msgList.RemoveAt(0);
                            String[] _CCmsg = _msgList.ToArray();
                            if (_CCmsg[0] == "REQ_ROUTE") {
                                IVertexAndEdgeListGraph<string, Edge<string>> graph = networkGraph;
                                string root = _CCmsg[1];
                                string target = _CCmsg[2];
                                if (_CCmsg.Length == 4)
                                {
                                    
                                    AdjacencyGraph<String, Edge<String>> _graph = copyGraph(networkGraph);
                                    _graph.RemoveVertex(_CCmsg[3]);
                                    graph = _graph;
                                }
                                numberOfRoutes++;
                                sendRoute[numberOfRoutes] = true;
                                calculatePath(graph, root, target, numberOfRoutes);
                            }
                            #endregion
                        } else {
                            #region FROM LRM
                            //_msgList.RemoveAt(0);
                            String[] _LRMmsg = _msgList.ToArray();
                            //gdy logowanie się LRM
                            if (_LRMmsg[0] == "HELLO") {
                                Address _addr;
                                if (Address.TryParse(_LRMmsg[1], out _addr)) {
                                    if (networkGraph.ContainsVertex(_addr.ToString())) {
                                        List<String> _params = new List<String>();
                                        _params.Add("ADDR_TAKEN");
                                        SPacket packet = new SPacket(myAddr.ToString(), _senderAddr.ToString(), _params);
                                        whatToSendQueue.Enqueue(packet);
                                    } else {
                                        networkGraph.AddVertex(_addr.ToString());
                                        if (isDebug) SetText("Dodano węzeł grafu");
                                        fillGraph();
                                        //List<String> _params = new List<String>();
                                        //_params.Add("REQ_TOPOLOGY");
                                        //SPacket packet = new SPacket(myAddr.ToString(), _senderAddr.ToString(), _params);
                                        //whatToSendQueue.Enqueue(packet);
                                    }
                                }
                            }
                            if (_LRMmsg[0] == "DEAD") {
                                Address deadAddr;
                                if (Address.TryParse(_LRMmsg[1], out deadAddr)) {
                                    if (networkGraph.ContainsVertex(deadAddr.ToString())) {
                                        networkGraph.RemoveVertex(deadAddr.ToString());
                                        fillGraph();
                                        SetText("Węzeł o adresie " + deadAddr.ToString() + " przestał działać, usuwam go z grafu");
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
                                        string destAddr;
                                        //gdy przyszło info o wezle z innej podsieci
                                        if (_destAddr.subnet != myAddr.subnet) {
                                            subnetConnections.Add(new KeyValuePair<Address,Address>(_senderAddr, _destAddr));
                                            //dodaje informację o tym że ta sieć jest osiągalna przeze mnie
                                            if (!availableSubnetworksViaMe.Contains(_destAddr.subnet)) {
                                                availableSubnetworksViaMe.Add(_destAddr.subnet);
                                            }
                                            destAddr = _destAddr.network + "." + _destAddr.subnet + ".*";
                                            List<String> temp = new List<string>();
                                            if (availableSubnetworks.ContainsKey(_senderAddr)) {
                                                if (availableSubnetworks.TryGetValue(_senderAddr, out temp)) {
                                                    availableSubnetworks.Remove(_senderAddr);
                                                }
                                            }
                                            temp.Add(destAddr);
                                            availableSubnetworks.Add(_senderAddr, temp);
                                        } else destAddr = _destAddr.ToString();
                                        
                                        //jeśli jest już taka ścieżka nic nie rób
                                        if (networkGraph.TryGetEdge(_senderAddr.ToString(), destAddr, out x)) {
                                        }
                                            //jeśli nie ma
                                        else {
                                            //jeśli nie ma w węzłach grafu węzła z topologii - dodaj go
                                            if (!networkGraph.Vertices.Contains(destAddr)) networkGraph.AddVertex(destAddr);
                                            //dodaj ścieżkę
                                            networkGraph.AddEdge(new Edge<String>(_senderAddr.ToString(), destAddr));
                                            if (isDebug) SetText("Dodano ścieżkę z " + _senderAddr.ToString() + " do " + destAddr);
                                            //rysuj graf
                                            fillGraph();
                                        }
                                    }
                                }
                            }
                            //gdy przyszła wiadomość że łącze jest wolne
                            if (_LRMmsg[0] == "YES") {
                                lock (_nodesInPath) {
                                    int _index = int.Parse(_LRMmsg[2]);
                                    if (_nodesInPath[_index].Contains(_LRMmsg[1])) _nodesInPath[_index].Remove(_LRMmsg[1]);
                                    string[] _msgArr = _LRMmsg[1].Split('.');
                                    string temp = _msgArr[0] + "." + _msgArr[1] + ".*";
                                    if (_nodesInPath[_index].Contains(temp)) _nodesInPath[_index].Remove(temp);
                                    if (_nodesInPath[_index].Count == 0) {
                                        List<string> _routeMsg = new List<string>();
                                        string ccAddr = myAddr.network + "." + myAddr.subnet + ".1";
                                        _routeMsg.Add("ROUTE");
                                        foreach (string str in nodesInPath[_index]) _routeMsg.Add(str);
                                        SPacket packet = new SPacket(myAddr.ToString(), ccAddr, _routeMsg);
                                        if (sendRoute[_index])
                                        {
                                            whatToSendQueue.Enqueue(packet);
                                            sendRoute[_index] = false;
                                        }
                                    }
                                }
                            }
                            //gdy brak zasobów
                            if (_LRMmsg[0] == "NO") {
                                lock (_nodesInPath) {
                                    int _index = int.Parse(_LRMmsg[2]);
                                    string _root = nodesInPath[_index][0];
                                    string _target = nodesInPath[_index][nodesInPath.Count-1];
                                    numberOfRoutes++;
                                    _nodesInPath[numberOfRoutes] = new List<string>();
                                    nodesInPath[numberOfRoutes] = new List<string>();
                                    //tymczasowy graf reprezentujący sieć bez zajętego łącza
                                    AdjacencyGraph<String, Edge<String>> _networkGraph = copyGraph(networkGraph);
                                    Edge<string> edge;
                                    _networkGraph.TryGetEdge(receivedPacket.getSrc(), _LRMmsg[1], out edge);
                                    _networkGraph.RemoveEdge(edge);
                                    //_networkGraph.RemoveEdge(new Edge<String>(receivedPacket.getSrc(), _LRMmsg[1]));
                                    //_networkGraph.RemoveEdge(new Edge<String>(_LRMmsg[1], receivedPacket.getSrc()));
                                    IVertexAndEdgeListGraph<string, Edge<string>> graph = _networkGraph;
                                    fillGraph();
                                    calculatePath(graph, _root, _target, numberOfRoutes);
                                }
                            }
                            #endregion
                        }
                    }
                } catch {
                    SetText("WUT");
                    if (++exceptionCount == 5) {
                        this.Invoke((MethodInvoker)delegate() {
                            isConnectedToCloud = false;
                            this.sendTopologyButton.Enabled = blockSending ? true : false;
                            conToCloudButton.Text = "Połącz";
                            SetText("Rozłączono!");
                            if (cloudSocket != null) cloudSocket.Close();
                        }); 
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
                    SPacket _pck = (SPacket)whatToSendQueue.Dequeue();
                    BinaryFormatter bformatter = new BinaryFormatter();
                    if (!blockSending) bformatter.Serialize(networkStream, _pck);
                    networkStream.Flush();
                    String[] _argsToShow = _pck.getParames().ToArray();
                    String argsToShow = "";
                    foreach (String str in _argsToShow) {
                        argsToShow += str+" ";
                    }
                    if (isDebug) SetText("Wysłano: " + _pck.getSrc() + ":" + _pck.getDest() + ":" + argsToShow);
                    Thread.Sleep(50);
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
                    Routix.ActiveForm.Text = "Routix " + myAddr.ToString();
                    return true;
                } else {
                    Routix.ActiveForm.Text = "Routix";
                    return false;
                } else { 
                Routix.ActiveForm.Text = "Routix";
                return false;
            }
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

        public void ChangeButton(bool enable) {
            if (this.sendTopologyButton.InvokeRequired) {
                SetButtonCallback d = new SetButtonCallback(ChangeButton);
                this.Invoke(d, new object[] { enable });
            } else {
                this.sendTopologyButton.Enabled = enable;
                //timer1 = new System.Windows.Forms.Timer(this.components);
                //this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
                this.timer1.Enabled = true;
                this.timer1.Start();
            }
        }
        #endregion
        #region graphs handling
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

        private void calculatePath(IVertexAndEdgeListGraph<string, Edge<string>> graph, string root, string target, int index) {
            if (root != target)
            {
                if (_nodesInPath.Count != index + 1) {
                    while (_nodesInPath.Count != index + 1) {
                        _nodesInPath.Add(new List<string>());
                    }
                }
                _nodesInPath[index] = new List<string>();
                //IVertexAndEdgeListGraph<string, Edge<string>> graph = networkGraph;
                Func<Edge<String>, double> edgeCost = e => 1; // constant cost

                Address targ = Address.Parse(target);
                if (targ.subnet != myAddr.subnet) target = targ.network + "." + targ.subnet + ".*";
                //string root = _CCmsg[1];
                TryFunc<string, System.Collections.Generic.IEnumerable<QuickGraph.Edge<string>>> tryGetPaths = graph.ShortestPathsDijkstra(edgeCost, root);
                //string target = _CCmsg[2];
                IEnumerable<Edge<string>> path;
                if (tryGetPaths(target, out path))
                {
                    lock (_nodesInPath)
                    { 
                        SetText("Wyznaczona trasa od " + root + " do " + target + ":");
                        if (nodesInPath.Count != index + 1) {
                            while (nodesInPath.Count != index + 1) {
                                nodesInPath.Add(new List<string>());
                            }
                        }
                        nodesInPath[index] = new List<string>();
                        nodesInPath[index].Add(path.First().Source);
                        foreach (Edge<string> edge in path)
                        {
                            Address src;
                            if (Address.TryParse(edge.Source, out src))
                            {
                                Address tempdest;
                                if (!Address.TryParse(edge.Target, out tempdest))
                                {
                                    String[] srcArr = edge.Source.Split('.');
                                    String[] destArr = edge.Target.Split('.');
                                    if (destArr[1] != srcArr[1] && int.Parse(srcArr[1]) == myAddr.subnet)
                                    {
                                        foreach (KeyValuePair<Address, Address> kvp in subnetConnections)
                                        {
                                            if (kvp.Key.ToString() == src.ToString())
                                            {
                                                nodesInPath[index].Add(kvp.Value.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                            SetText(edge.ToString());
                            nodesInPath[index].Add(edge.Target);
                            String[] _arr = edge.Target.Split('.');
                            if (_arr[2] != "*") _nodesInPath[index].Add(edge.Target);
                        }
                    }
                    //pyta każdego LRM o to, czy jest wolne łącze do LRM następnego w kolejce
                    //nie pyta się ostatniego LRM w ścieżce, zakładam że jak w jedną stronę jest połączenie to i w drugą jest
                    for (int i = 0; i < nodesInPath[index].Count - 1; i++)
                    {
                        string[] srcArr = nodesInPath[index][i].Split('.');
                        if (int.Parse(srcArr[1]) == myAddr.subnet)
                        {
                            List<String> _msg = new List<String>();
                            _msg.Add("IS_LINK_AVAILABLE");
                            _msg.Add(nodesInPath[index][i + 1]);
                            _msg.Add(String.Empty + index);
                            SPacket _pck = new SPacket(myAddr.ToString(), nodesInPath[index][i], _msg);
                            whatToSendQueue.Enqueue(_pck);
                        }
                    }
                }
                else
                {
                    //gdy nie ma ścieżki
                    string ccAddr = myAddr.network + "." + myAddr.subnet + ".1";
                    string _routeMsg = "NO_ROUTE";
                    SPacket packet = new SPacket(myAddr.ToString(), ccAddr, _routeMsg);
                    whatToSendQueue.Enqueue(packet);
                }
            }
            else
            {
                List<string> _msg = new List<string>();
                _msg.Add("ROUTE");
                _msg.Add(root);
                SPacket pck = new SPacket(myAddr.ToString(), myAddr.network + "." + myAddr.subnet + ".1", _msg);
                whatToSendQueue.Enqueue(pck);
            }
        }
        #endregion

        #region event handling
        private void timer1_Tick(object sender, EventArgs e) {
            /*if (!firstRun) {
                if (progressBar1.Value < 100) {
                    progressBar1.Value += 1;
                } else {
                    progressBar1.Value = 0;
                    blockSending = false;
                    ChangeButton(true);
                    timer1.Stop();
                    firstRun = true;
                }
            } else {
                if (progressBar1.Value < 100) {
                    progressBar1.Value += 10;
                } else {
                    progressBar1.Value = 0;
                    blockSending = true;
                    ChangeButton(false);
                    firstRun = false;
                }
            }*/
            if (progressBar1.Value < 100) {
                progressBar1.Value += 2;
            } else {
                progressBar1.Value = 0;
                blockSending = true;
                //ChangeButton(false);
                timer1.Stop();
                //firstRun = true;
            }
        }
        
        #endregion

        private void reqTopButton_Click(object sender, EventArgs e) {
            foreach (string addr in networkGraph.Vertices) {
                Address address;
                if(Address.TryParse(addr,out address)){
                    SPacket pck = new SPacket(myAddr.ToString(), address.ToString(), "REQ_TOPOLOGY");
                    whatToSendQueue.Enqueue(pck);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            this.Invoke((MethodInvoker)delegate() {
                this.sendTopologyButton.Enabled = blockSending ? true : false;
            }); 
            blockSending = blockSending ? false : true;
            //isConnectedToCloud = isConnectedToCloud ? false : true;
        }

        private AdjacencyGraph<String, Edge<String>> copyGraph(AdjacencyGraph<String, Edge<String>> graph)
        {
            AdjacencyGraph<String, Edge<String>> _graph = new AdjacencyGraph<string, Edge<string>>();
            foreach (String vertex in graph.Vertices)
            {
                _graph.AddVertex(vertex);
            }
            foreach (Edge<String> edge in graph.Edges)
            {
                _graph.AddEdge(edge);
            }
            return _graph;
        }
            
    }
}

