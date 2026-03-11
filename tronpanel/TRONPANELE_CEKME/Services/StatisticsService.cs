using System.Collections.Concurrent;

namespace TRONPANELE_CEKME.Services
{
    public interface IStatisticsService
    {
        void IncrementRequest();
        void IncrementSuccess(decimal amount);
        void IncrementFailure();
        void SetTargets(int count, decimal amount, decimal minAmount, decimal maxAmount);
        (int TotalRequests, double RPS, double RPM, int SuccessCount, decimal SuccessAmount, int FailureCount, int TargetCount, decimal TargetAmount, decimal MinAmount, decimal MaxAmount) GetStats();
    }

    public class StatisticsService : IStatisticsService
    {
        private int _totalRequests = 0;
        private int _successCount = 0;
        private decimal _successAmount = 0;
        private int _failureCount = 0;
        private int _targetCount = 0;
        private decimal _targetAmount = 0;
        private decimal _minAmount = 0;
        private decimal _maxAmount = 0;

        private readonly ConcurrentQueue<DateTime> _requestTimestamps = new();
        private readonly object _lock = new();

        public void IncrementRequest()
        {
            Interlocked.Increment(ref _totalRequests);
            _requestTimestamps.Enqueue(DateTime.Now);
            CleanOldTimestamps();
        }

        public void IncrementSuccess(decimal amount)
        {
            Interlocked.Increment(ref _successCount);
            lock (_lock)
            {
                _successAmount += amount;
            }
        }

        public void IncrementFailure()
        {
            Interlocked.Increment(ref _failureCount);
        }

        public void SetTargets(int count, decimal amount, decimal minAmount, decimal maxAmount)
        {
            _targetCount = count;
            _targetAmount = amount;
            _minAmount = minAmount;
            _maxAmount = maxAmount;
        }

        private void CleanOldTimestamps()
        {
            var minuteAgo = DateTime.Now.AddMinutes(-1);
            while (_requestTimestamps.TryPeek(out var timestamp) && timestamp < minuteAgo)
            {
                _requestTimestamps.TryDequeue(out _);
            }
        }

        public (int TotalRequests, double RPS, double RPM, int SuccessCount, decimal SuccessAmount, int FailureCount, int TargetCount, decimal TargetAmount, decimal MinAmount, decimal MaxAmount) GetStats()
        {
            CleanOldTimestamps();
            var now = DateTime.Now;
            var fiveSecondsAgo = now.AddSeconds(-5);
            var minuteAgo = now.AddMinutes(-1);

            var timestamps = _requestTimestamps.ToArray();
            
            // Son 5 saniyedeki istek sayısını 5.0'a bölerek ondalıklı RPS elde ediyoruz
            double rps = timestamps.Count(t => t >= fiveSecondsAgo) / 5.0;
            double rpm = timestamps.Count(t => t >= minuteAgo);

            return (_totalRequests, rps, rpm, _successCount, _successAmount, _failureCount, _targetCount, _targetAmount, _minAmount, _maxAmount);
        }
    }
}
