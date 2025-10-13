This program is thread-safe in its handling of the two counters (_counterA and _counterB)

•	Synchronization: Each counter has its own lock object (_lockA and _lockB). All accesses and modifications to the counters are wrapped in lock statements, ensuring that only one thread can read or write a counter at a time.
•	Threaded Updates: The counters are incremented in separate background tasks (threads), each using its own cancellation token and lock.
•	Safe Reads: The GetCounterA() and GetCounterB() methods also use locks, so reading the counters is thread-safe.
•	No Shared State Between Counters: Each counter and its lock are independent, so there’s no risk of cross-thread interference.