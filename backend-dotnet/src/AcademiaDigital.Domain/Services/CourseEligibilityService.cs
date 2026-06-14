using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Services;

public class CourseEligibilityService
{
    public bool IsApproved(EnrollmentStatus status)
        => status is EnrollmentStatus.Approved or EnrollmentStatus.Promoted;

    public bool IsInProgress(EnrollmentStatus status)
        => status is EnrollmentStatus.Enrolled or EnrollmentStatus.Regularized;

    public bool SatisfiesMinimumStatus(EnrollmentStatus? currentStatus, MinimumRequiredStatus requiredStatus)
    {
        if (currentStatus is null) return false;

        return requiredStatus switch
        {
            MinimumRequiredStatus.Approved => currentStatus is EnrollmentStatus.Approved or EnrollmentStatus.Promoted,
            MinimumRequiredStatus.Promoted => currentStatus == EnrollmentStatus.Promoted,
            MinimumRequiredStatus.Regularized => currentStatus is EnrollmentStatus.Regularized
                or EnrollmentStatus.Approved
                or EnrollmentStatus.Promoted,
            _ => false
        };
    }
}
