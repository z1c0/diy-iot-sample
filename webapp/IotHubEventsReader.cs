using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IotSensorWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;

namespace IotSensorWeb
{
  public class IoTHubEventsReader
  {
    // Event Hub-compatible endpoint
    // az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT Hub name}
    private const string EventHubsCompatibleEndpoint = "";
    // Event Hub-compatible name
    // az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT Hub name}
    private const string EventHubsCompatiblePath = "";
    // az iot hub policy show --name iothubowner --query primaryKey --hub-name {your IoT Hub name}
    private const string IotHubSasKey = "";
    private const string IotHubSasKeyName = "iothubowner";
    private static EventHubClient _eventHubClient;
    private static List<Task> _tasks;
    private readonly IServiceProvider _applicationServices;

    public IoTHubEventsReader(IServiceProvider applicationServices)
    {
      _applicationServices = applicationServices;
      Run();
    }

    // Asynchronously create a PartitionReceiver for a partition and then start 
    // reading any messages sent from the simulated client.
    private async Task ReceiveMessagesFromDeviceAsync(string partition)
    {
      // Create the receiver using the default consumer group.
      // For the purposes of this sample, read only messages sent since 
      // the time the receiver is created. Typically, you don't want to skip any messages.
      var eventHubReceiver = _eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
      while (true)
      {
        var events = await eventHubReceiver.ReceiveAsync(100);
        if (events != null)
        {
          foreach (var eventData in events)
          {
            var data = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(eventData.Body.Array));
            Console.WriteLine($"data: {data}");
            var hub = _applicationServices.GetService(typeof(IHubContext<SensorHub>)) as IHubContext<SensorHub>;
            await hub.Clients.All.SendAsync("Broadcast", "Hugo", data);
          }
        }
      }
    }

    private async void Run()
    {
      // Create an EventHubClient instance to connect to the
      // IoT Hub Event Hubs-compatible endpoint.
      var connectionString = new EventHubsConnectionStringBuilder(new Uri(EventHubsCompatibleEndpoint), EventHubsCompatiblePath, IotHubSasKeyName, IotHubSasKey);
      _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());

      // Create a PartitionReciever for each partition on the hub.
      var runtimeInfo = await _eventHubClient.GetRuntimeInformationAsync();
      var d2cPartitions = runtimeInfo.PartitionIds;

      _tasks = new List<Task>();
      foreach (string partition in d2cPartitions)
      {
        _tasks.Add(ReceiveMessagesFromDeviceAsync(partition));
      }
    }
  }
}