#include <time.h>
#include <ESP8266WiFi.h>
//#include <WiFiClientSecure.h>
//#include <WiFiUdp.h>
#include <AzureIoTHub.h>
#include <AzureIoTProtocol_HTTP.h>
#include "DHT.h"

// Times before 2010 (1970 + 40 years) are invalid
#define MIN_EPOCH 40 * 365 * 24 * 3600

//
// TODO: set these values before uploading the sketch
//
#define IOT_CONFIG_WIFI_SSID            ""
#define IOT_CONFIG_WIFI_PASSWORD        ""
#define IOT_CONFIG_CONNECTION_STRING    ""


char msg[100];

#define DHTPIN 2
DHT dht(DHTPIN, DHT11);

IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle = nullptr;


void initWifi() {
    // Attempt to connect to Wifi network:
    Serial.print("\r\n\r\nAttempting to connect to SSID: ");
    Serial.println(IOT_CONFIG_WIFI_SSID);
    
    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    WiFi.begin(IOT_CONFIG_WIFI_SSID, IOT_CONFIG_WIFI_PASSWORD);
    while (WiFi.status() != WL_CONNECTED) {
      delay(500);
      Serial.print(".");
    }
    
    Serial.println("\r\nConnected to wifi");
}

static void initTime() {  
   time_t epochTime;

   configTime(0, 0, "pool.ntp.org", "time.nist.gov");

   while (true) {
       epochTime = time(NULL);

       if (epochTime < MIN_EPOCH) {
           Serial.println("Fetching NTP epoch time failed! Waiting 2 seconds to retry.");
           delay(2000);
       } else {
           Serial.print("Fetched NTP epoch time is: ");
           Serial.println(epochTime);
           break;
       }
   }
}


void setup()
{
    Serial.begin(115200);
    //
    // Init DHT11 sensor
    //
    dht.begin(); //DHT11 Sensor starten
    //
    // Init WIFI
    //
    initWifi();
    //
    // Init time
    //
    initTime();
    //
    // Init IoT client
    //
    iotHubClientHandle = IoTHubClient_LL_CreateFromConnectionString(IOT_CONFIG_CONNECTION_STRING, HTTP_Protocol);
    if (iotHubClientHandle == NULL)
    {
        Serial.println("Failed on IoTHubClient_LL_Create");
    }
}

void loop()
{
  sprintf(msg, "{ 'humidity': %.2f,'temperature':%.2f }", dht.readHumidity(), dht.readTemperature());  
  IOTHUB_MESSAGE_HANDLE messageHandle = IoTHubMessage_CreateFromByteArray((const unsigned char *)msg, strlen(msg));
  if (messageHandle == NULL)
  {
    Serial.println("unable to create a new IoTHubMessage");
  }
  else
  {                           
    if (IoTHubClient_LL_SendEventAsync(iotHubClientHandle, messageHandle, nullptr, nullptr) != IOTHUB_CLIENT_OK)
    {
      Serial.println("failed to hand over the message to IoTHubClient");
    }
    else
    {
      Serial.println("IoTHubClient accepted the message for delivery");
    }
    IoTHubMessage_Destroy(messageHandle);
  }
  
  IOTHUB_CLIENT_STATUS status;
  while ((IoTHubClient_LL_GetSendStatus(iotHubClientHandle, &status) == IOTHUB_CLIENT_OK) && (status == IOTHUB_CLIENT_SEND_STATUS_BUSY))
  {
      IoTHubClient_LL_DoWork(iotHubClientHandle);
      ThreadAPI_Sleep(100);
  }
  
  Serial.println("wait...");
  delay(5000);
}



