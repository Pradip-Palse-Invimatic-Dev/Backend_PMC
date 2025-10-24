namespace MyWebApp.Models.Enums
{
    public enum ApplicationStatus
    {
        Draft,
        Submitted,
        UnderReview,
        DocumentVerificationPending,
        DocumentVerified,
        AppointmentScheduled,
        AppointmentCompleted,
        JuniorEngineerApproved,
        AssistantEngineerApproved,
        ExecutiveEngineerApproved,
        CityEngineerApproved,
        PaymentPending,
        PaymentCompleted,
        ClerkApproved,
        DigitallySignedByExecutive,
        DigitallySignedByCity,
        Completed,
        Rejected
    }

    public enum PositionType
    {
        Architect,
        StructuralEngineer,
        LicenceEngineer,
        Supervisor1,
        Supervisor2
    }

    public enum ApplicationStage
    {
        JUNIOR_ENGINEER_PENDING,
        DOCUMENT_VERIFICATION_PENDING,
        ASSISTANT_ENGINEER_PENDING,
        EXECUTIVE_ENGINEER_PENDING,
        CITY_ENGINEER_PENDING,
        PAYMENT_PENDING,
        CLERK_PENDING,
        EXECUTIVE_ENGINEER_SIGN_PENDING,  //for digital signature
        CITY_ENGINEER_SIGN_PENDING,       //for digital signature
        APPROVED,
        REJECTED
    }
}
