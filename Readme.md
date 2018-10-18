# Azure IoT Edge Dweet module

This is a simple [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/) custom module, that forwards all messages sent to it to the [Dweet.io](https://dweet.io) service. Currently only sending 'flat' json message payloads is supported:

```
{
    "temperature" : 12.21,
    "humidity" : 35.22
}
```
Only messages sent to 'input1' of the module will be forwarded accordingly. As an example this project contains a temperature simulator module, too, that can be used for testing.

## Configuration

### Dweet thing name
The thing name used on the [Dweet.io](https://dweet.io) web portal can be configured by either specifying it as the first argument of the module or by setting the environment variable DWEETTHINGNAME. If neither is specified, the name of the current Azure IoT Hub instance will be used. Anyway you can always find the used thing name in the docker log of the module.

### Route for Azure IoT Edge

Sample:
```
        "routes": {
          "sensorToDweetModule": "FROM /messages/modules/TemperatureSimulatorModule/outputs/* INTO BrokeredEndpoint(\"/modules/DweetModule/inputs/input1\")",
          "TemperatureSimulatorModuleToIoTHub": "FROM /messages/modules/TemperatureSimulatorModule/outputs/* INTO $upstream"
        },
```