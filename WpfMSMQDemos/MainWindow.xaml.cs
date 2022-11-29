using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfMSMQDems.Models;

namespace WpfMSMQDems
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }
    private void CreateQueue_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var mq = MessageQueue.Create(@".\private$\ResponseFlightQueue"))
        {
          LBMessages.Items.Add($"Created Queue: {mq.QueueName}");
        }
      }
      catch (Exception ex)
      {

        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }

    private void FindPrivate_Click(object sender, RoutedEventArgs e)
    {
      MessageQueue[] mqs = MessageQueue.GetPrivateQueuesByMachine(".");

      foreach (var item in mqs)
      {
        LBMessages.Items.Add(item.QueueName);
      }
    }
    bool highlow = true;
    private void SendMessage_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
        {
          using (var responseQueue = new MessageQueue(@".\private$\ResponseFlightQueue"))
          {

            var fl = new Flight() { Id = 1, FlightNo = "AL321", Origin = "Heathrow", Destination = "Rome" };
            Message msg = new Message();

            msg.Body = fl;
            msg.Label = "Flight";

            msg.Priority = highlow ? MessagePriority.High : MessagePriority.Low;
            highlow = !highlow;

            msg.AdministrationQueue = new MessageQueue(@".\private$\AnotherFlightQueue_Ack");
            msg.AcknowledgeType = AcknowledgeTypes.PositiveArrival;

            msg.ResponseQueue = responseQueue;
            flightsQueue.Send(msg);

            LBMessages.Items.Add($"Placed the flight: {fl}");
          }
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }
    private void SendBodyStream_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var mq = new MessageQueue(@".\private$\AnotherBodyQueue"))
        {
          using (Stream s = new FileStream("C:/Temp/MSMQ/foxtext.txt", FileMode.Open))
          {
            Message msg = new Message();
            msg.BodyStream = s;

            msg.Label = "BrownFox";

            mq.Send(msg);
            LBMessages.Items.Add($"Added BodyStream message");
          }
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }
    private void ReceiveBodyStream_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var mq = new MessageQueue(@".\private$\AnotherBodyQueue"))
        {
          Message msg = mq.Receive();

          using (Stream s = msg.BodyStream)
          {
            using (StreamReader sr = new StreamReader(s))
            {
              LBMessages.Items.Add(sr.ReadLine());
            }
          }
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }
    private void PeekFlightMessage_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
        {
          Message msg = flightsQueue.Peek(TimeSpan.FromMinutes(1));
          msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

          LBMessages.Items.Add($"Peeked message; {msg.Label}, {msg.Body}");
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }
    private void ReceiveFlightMessage_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
        {
          Message msg = flightsQueue.Receive(TimeSpan.FromMinutes(1));
          msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

          LBMessages.Items.Add($"Receive message; {msg.Label}, {msg.Body}");
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }

    private void PeekAsynchronous_Click(object sender, RoutedEventArgs e)
    {
      using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
      {
        flightsQueue.PeekCompleted += FlightsQueue_PeekCompleted;

        try
        {
          IAsyncResult ar = flightsQueue.BeginPeek(TimeSpan.FromMinutes(1), this);
        }
        catch (Exception ex)
        {
          LBMessages.Items.Add($"Peek timeout: {ex.Message}");
        }
      }
    }
    private void FlightsQueue_PeekCompleted(object sender, PeekCompletedEventArgs e)
    {
      try
      {
        MessageQueue mq = sender as MessageQueue;

        var msg = mq.EndPeek(e.AsyncResult);
        msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

        this.Dispatcher.BeginInvoke(new Action<Message>(m =>
        {
          LBMessages.Items.Add($"Message available: {m.Label}, {m.Body}");
        }), msg);
      }
      catch (Exception ex)
      {
        this.Dispatcher.BeginInvoke(new Action(() =>
        {
          LBMessages.Items.Add($"Problem: No message! {ex.Message}");
        }));
      }
    }
    private void ReceiveAsynchronous_Click(object sender, RoutedEventArgs e)
    {
      using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
      {
        flightsQueue.ReceiveCompleted += FlightsQueue_ReceiveCompleted;
        try
        {
          IAsyncResult ar = flightsQueue.BeginReceive(TimeSpan.FromMinutes(2), this);
        }
        catch (Exception ex)
        {
          LBMessages.Items.Add($"Receive timeout: {ex.Message}");
        }
      }
    }
    private void FlightsQueue_ReceiveCompleted(object sender, ReceiveCompletedEventArgs e)
    {
      try
      {
        MessageQueue mq = sender as MessageQueue;

        var msg = mq.EndReceive(e.AsyncResult);
        msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

        this.Dispatcher.BeginInvoke(new Action<Message>(m =>
        {
          LBMessages.Items.Add($"Message received: {m.Label}, {m.Body}");
        }), msg);
        mq.BeginReceive();// TimeSpan.FromMinutes(2), this);
      }
      catch (Exception ex)
      {
        this.Dispatcher.BeginInvoke(new Action(() =>
        {
          LBMessages.Items.Add($"Problem: No message! {ex.Message}");
        }));
      }
    }

    private void Enumerate_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        LBMessages.Items.Add($"Beginning enumeration...");
        using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
        {
          var enumerator = flightsQueue.GetMessageEnumerator2();
          flightsQueue.MessageReadPropertyFilter.Priority = true;

          while (enumerator.MoveNext())
          {
            var msg = enumerator.Current;
            msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

            LBMessages.Items.Add($"Label: {msg.Label}, Priority: {msg.Priority}, received: {msg.Body}");
          }
        }
        LBMessages.Items.Add($"End of enumeration...");
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }

    private void EnumeratorRemove_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        LBMessages.Items.Add($"Beginning remove enumeration...");
        using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
        {
          var enumerator = flightsQueue.GetMessageEnumerator2();

          while (enumerator.MoveNext())
          {
            var msg = enumerator.RemoveCurrent();
            msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

            LBMessages.Items.Add($"Label: {msg.Label}, received: {msg.Body}");

            enumerator.Reset();
          }
        }
        LBMessages.Items.Add($"End remove enumeration...");
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }

    private void SendTransaction_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var fl1 = new Flight() { Id = 1, FlightNo = "AL321", Origin = "Heathrow", Destination = "Rome" };
        var fl2 = new Flight() { Id = 2, FlightNo = "AL322", Origin = "Rome", Destination = "Heathrow" };
        using (var flightsQueue = new MessageQueue(@".\private$\TransactionFlightQueue"))
        {
          using (MessageQueueTransaction trns = new MessageQueueTransaction())
          {
            trns.Begin();
            {
              Message msg = new Message();

              msg.Body = fl1;
              msg.Label = "First flight";

              flightsQueue.Send(msg,trns);
            }
            //throw new OverflowException();
            {
              Message msg = new Message();

              msg.Body = fl2;
              msg.Label = "First flight";

              flightsQueue.Send(msg,trns);
            }

            LBMessages.Items.Add($"Placed the flight: {fl1}");
            LBMessages.Items.Add($"Placed the flight: {fl2}");
            trns.Commit();
          }
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }

    private void ReceiveTransaction_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var flightsQueue = new MessageQueue(@".\private$\TransactionFlightQueue"))
        {
          using (MessageQueueTransaction trns = new MessageQueueTransaction())
          {
            trns.Begin();
            Message msg1 = flightsQueue.Receive(trns);
            msg1.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });
            throw new OverflowException();
            Message msg2 = flightsQueue.Receive(trns);
            msg2.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

            LBMessages.Items.Add($"Receive message; {msg1.Label}, {msg1.Body}");
            LBMessages.Items.Add($"Receive message; {msg2.Label}, {msg2.Body}");
            trns.Commit();
          }
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }

    private void ReceiveWithResponse_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (var flightsQueue = new MessageQueue(@".\private$\AnotherFlightQueue"))
        {
          Message msg = flightsQueue.Receive(TimeSpan.FromMinutes(1));
          msg.Formatter = new XmlMessageFormatter(new Type[] { typeof(Flight) });

          msg.ResponseQueue.Send("Received Flight", "OK");

          LBMessages.Items.Add($"Receive message; {msg.Label}, {msg.Body}");
        }
      }
      catch (Exception ex)
      {
        LBMessages.Items.Add($"Problem: {ex.Message}");
      }
    }
  }
}
