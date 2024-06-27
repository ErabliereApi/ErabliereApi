import paho.mqtt.client as mqtt

# MQTT broker details
broker_address = "localhost"
broker_port = 1883
username = "your_username"
password = "your_password"

# Callback functions
def on_connect(client, userdata, flags, rc):
    if rc == 0:
        print("Connected to MQTT broker")
        # Subscribe to a topic
        client.subscribe("your/topic")
    else:
        print("Failed to connect, return code: ", rc)

def on_message(client, userdata, msg):
    print("Received message: ", msg.payload.decode())

def on_disconnect(client, userdata, rc):
    print("Disconnected from MQTT broker")

# Create MQTT client instance
client = mqtt.Client()

# Set callback functions
client.on_connect = on_connect
client.on_message = on_message
client.on_disconnect = on_disconnect

# Set username and password (if required)
client.username_pw_set(username, password)

# Connect to MQTT broker
client.connect(broker_address, broker_port)

# Start the MQTT client loop
client.loop_start()

# Publish a message
client.publish("your/topic", "Hello, MQTT!")

# Wait for messages
try:
    while True:
        pass
except KeyboardInterrupt:
    pass

# Disconnect from MQTT broker
client.loop_stop()
client.disconnect()