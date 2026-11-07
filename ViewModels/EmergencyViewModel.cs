using System.Collections.Generic;
using WeightBalance.Models;

namespace WeightBalance.ViewModels;

// Read-only: the emergency checklists live in EmergencyProcedures.
public sealed class EmergencyViewModel
{
    public string Source => EmergencyProcedures.Source;
    public string Priorities => EmergencyProcedures.Priorities;
    public IReadOnlyList<ProcedureGroup> Groups => EmergencyProcedures.Groups;
}
