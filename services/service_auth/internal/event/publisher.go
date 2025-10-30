package event

import "log"

// Publisher publishes authentication events to a message bus. The PDF
// mentions sending events like USER_REGISTERED, LOGIN_SUCCESS, etc. via
// Kafka【376788227130253†screenshot】. This interface abstracts over the
// concrete broker implementation.
type Publisher interface {
    Publish(eventType string, payload any) error
}

// StdoutPublisher is a simple implementation that logs events. It can be
// replaced with a KafkaPublisher or any other broker.
type StdoutPublisher struct{}

func (StdoutPublisher) Publish(eventType string, payload any) error {
    log.Printf("EVENT %s: %v", eventType, payload)
    return nil
}