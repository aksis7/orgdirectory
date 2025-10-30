package event

import (
    "context"
    "encoding/json"
    "log"

    "github.com/segmentio/kafka-go"
)

// KafkaPublisher publishes events to a Kafka topic. It uses the
// github.com/segmentio/kafka-go library, which is simple and idiomatic.
// In production you may need to tune writer configuration (batch size,
// compression, etc.)【888612807761432†screenshot】.
type KafkaPublisher struct {
    writer *kafka.Writer
    topic  string
}

// NewKafkaPublisher constructs a KafkaPublisher. `brokers` is a slice
// containing bootstrap broker addresses (e.g., []string{"kafka:9092"}).
// `topic` is the Kafka topic to publish events to.
func NewKafkaPublisher(brokers []string, topic string) *KafkaPublisher {
    w := &kafka.Writer{
        Addr:         kafka.TCP(brokers...),
        Topic:        topic,
        Balancer:     &kafka.LeastBytes{},
        RequiredAcks: kafka.RequireOne,
    }
    return &KafkaPublisher{writer: w, topic: topic}
}

// Publish sends a message to Kafka. The eventType is included in the
// message headers and payload is JSON‑encoded【376788227130253†screenshot】.
func (k *KafkaPublisher) Publish(eventType string, payload any) error {
    body, err := json.Marshal(payload)
    if err != nil {
        return err
    }
    msg := kafka.Message{
        Key:   []byte(eventType),
        Value: body,
        Headers: []kafka.Header{
            {Key: "event_type", Value: []byte(eventType)},
        },
    }
    if err := k.writer.WriteMessages(context.Background(), msg); err != nil {
        return err
    }
    log.Printf("published event %s to topic %s", eventType, k.topic)
    return nil
}

// Close shuts down the writer, flushing any buffered messages.
func (k *KafkaPublisher) Close() error {
    return k.writer.Close()
}