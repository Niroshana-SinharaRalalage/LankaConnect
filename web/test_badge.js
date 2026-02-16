// Quick test in browser console
const RegistrationStatus = {
  Preliminary: 0,
  Confirmed: 1,
  Attended: 2,
  NoShow: 3,
  Cancelled: 4
};

const event = {
  userRegistrationStatus: "Confirmed"  // String from API
};

console.log('API value:', event.userRegistrationStatus);
console.log('Enum value:', RegistrationStatus.Confirmed);
console.log('Are they equal?:', event.userRegistrationStatus === RegistrationStatus.Confirmed);
console.log('String comparison:', event.userRegistrationStatus === "Confirmed");
console.log('Typeof API value:', typeof event.userRegistrationStatus);
console.log('Typeof Enum value:', typeof RegistrationStatus.Confirmed);
