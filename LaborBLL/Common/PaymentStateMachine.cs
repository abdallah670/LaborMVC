using LaborDAL.Enums;

namespace LaborBLL.Common
{
    /// <summary>
    /// State machine for payment status transitions
    /// Ensures valid state changes and prevents invalid transitions
    /// </summary>
    public static class PaymentStateMachine
    {
        private static readonly Dictionary<PaymentStatus, PaymentStatus[]> ValidTransitions = new()
        {
            [PaymentStatus.Pending] = new[] { PaymentStatus.Held, PaymentStatus.Failed },
            [PaymentStatus.Held] = new[] { PaymentStatus.Released, PaymentStatus.Refunded, PaymentStatus.PartiallyRefunded },
            [PaymentStatus.Released] = Array.Empty<PaymentStatus>(),
            [PaymentStatus.Refunded] = Array.Empty<PaymentStatus>(),
            [PaymentStatus.PartiallyRefunded] = Array.Empty<PaymentStatus>(),
            [PaymentStatus.Failed] = Array.Empty<PaymentStatus>()
        };

        /// <summary>
        /// Determines if a transition from one status to another is valid
        /// </summary>
        public static bool CanTransition(PaymentStatus fromStatus, PaymentStatus toStatus)
        {
            if (fromStatus == toStatus)
                return true; // Same state is always valid (idempotent)

            return ValidTransitions.TryGetValue(fromStatus, out var validStates) 
                && validStates.Contains(toStatus);
        }

        /// <summary>
        /// Gets the list of valid next states from the current state
        /// </summary>
        public static PaymentStatus[] GetValidTransitions(PaymentStatus currentStatus)
        {
            return ValidTransitions.TryGetValue(currentStatus, out var validStates) 
                ? validStates 
                : Array.Empty<PaymentStatus>();
        }

        /// <summary>
        /// Validates a state transition and throws an exception if invalid
        /// </summary>
        public static void ValidateTransition(PaymentStatus fromStatus, PaymentStatus toStatus)
        {
            if (!CanTransition(fromStatus, toStatus))
            {
                var validStates = GetValidTransitions(fromStatus);
                var validStatesString = validStates.Length > 0 
                    ? string.Join(", ", validStates) 
                    : "no transitions allowed (terminal state)";
                
                throw new InvalidOperationException(
                    $"Invalid payment status transition from '{fromStatus}' to '{toStatus}'. " +
                    $"Valid transitions from '{fromStatus}' are: {validStatesString}");
            }
        }
    }
}
