namespace VGoalsPlanner.Domain.Enums;

public enum VTaskElaborationLevel
{
    Reminder = 0,   // Напоминание (0-30%)
    Research = 1,   // Исследование (30-70%)
    Actionable = 2, // Готова к работе (70-90%)
    Detailed = 3    // Детально проработана (90-100%)
}
