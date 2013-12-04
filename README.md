See An [Gurux](http://www.gurux.org/ "Gurux") for an overview.

Join the Gurux Community or follow [@Gurux](https://twitter.com/guruxorg "@Gurux") for project updates.

Open Source GXSMS (Short Message Service) media component, made by Gurux Ltd, is a part of GXMedias set of media components, which programming interfaces help you implement communication by chosen connection type. Gurux media components also support the following connection types: network (TCP/IP and UDP), serial port and terminal.

For more info check out [Gurux](http://www.gurux.org/ "Gurux").

We are updating documentation on Gurux web page. 

If you have problems you can ask your questions in Gurux [Forum](http://www.gurux.org/forum).

Simple example
=========================== 
Before use you must set following settings:
* PhoneNumber
* PortName
* BaudRate
* DataBits
* Parity
* StopBits
* Pin code

It is also good to listen following events:
* OnError
* OnReceived

```csharp

GXSMS cl = new GXSMS();
cl.PhoneNumber = "Phone number";
cl.PortName = "COM1";
cl.BaudRate = 9600;
cl.DataBits = 8;
cl.Parity = System.IO.Ports.Parity.None;
cl.StopBits = System.IO.Ports.StopBits.One;
cl.Open();

```

SMS message is send with send command:

```csharp
Gurux.SMS.GXSMSMessage msg = new Gurux.SMS.GXSMSMessage();
msg.Data = "Hello World!";
msg.Number = "Phone number";
cl.Send(msg);
```

In default mode received SMS message is coming as asynchronously from OnReceived event.

```csharp
cl.OnReceived += new ReceivedEventHandler(this.OnReceived);

```

```csharp
/// <summary>
/// Show received SMS message.
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>        
private void cl_OnReceived(object sender, ReceiveEventArgs e)
{
	try
	{
        GXSMSMessage msg = e.Data as GXSMSMessage;
        ListViewItem it = MsgList.Items.Add(msg.Data);
        it.SubItems.Add(msg.PhoneNumber);
        it.SubItems.Add(msg.Time.ToString());
	}
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

Data can be send as syncronous if needed:

```csharp
lock (cl.Synchronous)
{
	Gurux.SMS.GXSMSMessage msg = new Gurux.SMS.GXSMSMessage();
	msg.Data = "Hello World!";
	msg.Number = "Phone number";
	cl.Send(msg);
	//Wait answer for 60 seconds.
	Gurux.SMS.GXSMSMessage reply = cl.Receive(60)
}
```