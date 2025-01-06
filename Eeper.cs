namespace LeFunnyAI {
    /// This is an eeper. Its purpose is to set a minimum delay on operations.
    /// It can be set, run some code (e.g. run AI) and wait until a minimum delay so it doesn't look like it's thinking inhumanly quickly if the AI is super fast.
    public class Eeper {
        DateTime eepStart;

        public void StartEeper() {
            eepStart = DateTime.UtcNow;
        }

        public void EepTo(int ms) {
            double waitMs = eepStart.AddMilliseconds(ms).Subtract(DateTime.UtcNow).TotalMilliseconds;
            if (waitMs > 1) {
                Thread.Sleep((int)waitMs);
            }
        }
    }
}