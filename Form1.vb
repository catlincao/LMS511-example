Imports ServiceStack.Redis
Imports System.Threading
Imports System.IO





Public Class Form1

    'Public Shared lms As New Form1

#Region "增加redis发布订阅功能"
    Public Class SubscriptionMessageEventArgs
        Inherits EventArgs  '继承EventArgs

        Public Property Key As String
        Public Property value As String

        ''' <summary>
        ''' Initializes a new instance of the SubscriptionMessageEventArgs class.
        ''' </summary>
        Public Sub New(ByVal key As String, ByVal value As String)
            Me.Key = key
            Me.value = value
        End Sub
    End Class

    ''' <summary>
    ''' The redis store encapsulation class around the ServiceStack redis client 
    ''' </summary>
    ''' <remarks>This class is cumulatively constructed 
    ''' across the tutorial and is not broken.
    ''' </remarks>
    '''    
    Public Class RedisStore


#Region " Properties "

        Private _sourceClient As RedisClient
        Public ReadOnly Property SourceClient() As RedisClient
            Get
                Return _sourceClient
            End Get
        End Property

#End Region

#Region " Event "

        Public Event OnSubscriptionMessage As EventHandler(Of SubscriptionMessageEventArgs)
#End Region

#Region " Constructors "

        Public Sub New()

            MyClass.New(False)
        End Sub

        Public Sub New(ByVal ForceCheckServer As Boolean)
            _sourceClient = New RedisClient
            If ForceCheckServer AndAlso Not IsServerAlive() Then
                Throw New Exception("The server has not been started!")
            End If
        End Sub

        '内部类与外部类对象的数据传递
        Dim lms As Form1
        Public Sub New(ByVal p As Form1)
            MyClass.New(False)
            lms = p
        End Sub

#End Region

        Public Function IsServerAlive() As Boolean
            Try
                Return SourceClient.Ping
            Catch ex As Exception
                Return False
            End Try
        End Function

#Region " Functionalities "

#Region " Get/Set Keys "

        Public Function SetKey(ByVal key As String, ByVal value As String) As Boolean
            Return SourceClient.Set(key, value)
        End Function

        Public Function SetKey(Of T)(ByVal key As String, ByVal value As T) As Boolean
            Return SourceClient.Set(Of T)(key, value)
        End Function

        Public Function GetKey(ByVal key As String) As String
            Return Helper.GetString(SourceClient.Get(key))
        End Function

        Public Function GetKey(Of T)(ByVal key As String) As T
            Return SourceClient.Get(Of T)(key)
        End Function

#End Region

#Region " Pub/Sub "
        Public Sub Subscribe(ByVal ParamArray channels() As String)
            Dim SubClient As IRedisSubscription = SourceClient.CreateSubscription
            SubClient.OnMessage = New Action(Of String, String)(AddressOf OnMessage)
            SubClient.SubscribeToChannels(channels)
        End Sub


        Private Sub OnMessage(ByVal channel As String, ByVal value As String)

            RaiseEvent OnSubscriptionMessage(Me, New SubscriptionMessageEventArgs(channel, value))

            If value = "hello" Then
                CheckForIllegalCrossThreadCalls = False '忽略程序跨越线程运行导致的错误。  
                lms.Button2.Enabled = False
                lms.test()
            End If
        End Sub

        Public Sub Publish(ByVal channel As String, ByVal value As String)
            SourceClient.Publish(channel, Helper.GetBytes(value))
        End Sub

#End Region

#Region "定义多线程"
        Private Delegate Function subDelegate(ByVal objParam As Object) As String
        Public Function subIf(ByVal objParam As Object) As String
            Dim subCall As New subDelegate(AddressOf subc)
            subCall.BeginInvoke(objParam, Nothing, Nothing)
            Return ""
        End Function

        Private Function subc(ByVal objParam As Object) As String

            Subscribe("hi")
            Return ""
        End Function
#End Region
#End Region

    End Class

    Public Class Helper

        Private Shared ReadOnly UTF8EncObj As New System.Text.UTF8Encoding()

        Public Shared Function GetBytes(ByVal source As Object) As Byte()
            Return UTF8EncObj.GetBytes(source)
        End Function

        Public Shared Function GetString(ByVal sourceBytes As Byte()) As String
            Return UTF8EncObj.GetString(sourceBytes)
        End Function

    End Class



#Region "发布/订阅"

    'Private Sub PUB_Click(sender As Object, e As EventArgs) Handles PUB.Click
    '    pubsub.Publish("hi", "hello world")
    'End Sub

    'Private Sub subs_Click(sender As Object, e As EventArgs) Handles subs.Click
    '    subIf(sender)

    'End Sub
#End Region

#End Region
    


    'Flag which indicates if the ipadress in the textbox is valid or not
    Public IPaddressValidated As Boolean = True

    'Checking the validation of the IP-address
    Private TestIP As System.Net.IPAddress
    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        If System.Net.IPAddress.TryParse(TextBox1.Text, TestIP) = True Then
            TextBox1.BackColor = Color.Lime
            IPaddressValidated = True
        Else
            TextBox1.BackColor = Color.Red
            IPaddressValidated = False
        End If
    End Sub

    Public LMS_TCPconnection As New SICK_Communication.Ethernet.TCP_client
    Public LMS_DataProcessor As New SICK_LMS5xx_PRO_Library.DataProcessing
    Public LMS_ReturnString As String
    Public LMS_MEasurementStructure As New SICK_Work.SICK_Laserdevices.Scandata
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Connect

        If Button1.Text = "Connect" Then

            'If this IPaddress is valid and the device is online
            If IPaddressValidated = True And My.Computer.Network.Ping(TextBox1.Text) = True Then
                LMS_TCPconnection.Client_Connect(TextBox1.Text, 2111)  REM 'tcp port ''

                'enable the buttons
                Me.Button2.Enabled = True
                Me.TextBox1.Enabled = False

                Button1.Text = "Disconnect"

            End If

        Else
            'Disconnect
            LMS_TCPconnection.Client_Disconnect()
            Button2.Enabled = False
            TextBox1.Enabled = True
            Button1.Text = "Connect"
        End If

    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'Send the measurement request
        LMS_TCPconnection.SendString(SICK_LMS5xx_PRO_Library.DeviceFunctions.GetMeasurement, SICK_Communication.Ethernet.TCP_client.FramingTypes.Stx_Etx)

        'Wait for the Device to respond
        Do Until LMS_TCPconnection.Available > 0
        Loop

        ' Read the string from the sensor
        LMS_ReturnString = LMS_TCPconnection.Readavailable_string

        'Convert to the SICK_Work structure
        LMS_MEasurementStructure = LMS_DataProcessor.Process_SRA_LMDscandata(LMS_ReturnString)

        'Add the telegram to the form
        TextBox2.Text = LMS_ReturnString
        'StartTimer()
    End Sub

    Public Sub test()
        'Send the measurement request
        LMS_TCPconnection.SendString(SICK_LMS5xx_PRO_Library.DeviceFunctions.GetMeasurement, SICK_Communication.Ethernet.TCP_client.FramingTypes.Stx_Etx)

        'Wait for the Device to respond
        Do Until LMS_TCPconnection.Available > 0
        Loop

        ' Read the string from the sensor
        LMS_ReturnString = LMS_TCPconnection.Readavailable_string

        'Convert to the SICK_Work structure
        LMS_MEasurementStructure = LMS_DataProcessor.Process_SRA_LMDscandata(LMS_ReturnString)

        'Add the telegram to the form
        TextBox2.Text = LMS_ReturnString

        '保存数据到文本
        Dim PathUserData, PathReadData As String
        PathReadData = Application.StartupPath & "flag.txt"
        Dim flag As Integer
        flag = System.IO.File.ReadAllText(PathReadData)
        PathUserData = Application.StartupPath & CStr(flag) & ".txt"
        flag = flag + 1
        Dim t As System.IO.StreamWriter = New System.IO.StreamWriter(PathUserData, True, System.Text.Encoding.UTF8)
        t.Write(TextBox2.Text)
        t.Close()
        Dim flagText As System.IO.StreamWriter = New System.IO.StreamWriter(PathReadData, False, System.Text.Encoding.UTF8)
        flagText.Write(CStr(flag))
        flagText.Close()

    End Sub





    Public Sub StartTimer()
        Dim tcb As New TimerCallback(AddressOf Me.test)
        Dim objTimer As Timer
        objTimer = New Timer(tcb, Nothing, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(100))
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim pub_sub As RedisStore = New RedisStore(Me)
        Me.TextBox1.Text = "192.168.3.201"
        pub_sub.subIf(sender)
    End Sub

End Class
