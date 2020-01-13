using Com.Itg.JobCredits.Business;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NetStandardCompatibility.Com.Itg.Common;
using SessionManager;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using WebAPI.IVR.Attributes;
using WebAPI.IVR.Models.V1;
using static Com.Itg.JobCredits.Business.Screening;

namespace WebAPI.IVR.Controllers.V1
{
    public class IVRController : Controller
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string CsrUrlBase = GetUrlBase();


        //*********************************************************************************************
        private IConfiguration _configuration;

        public IVRController(IConfiguration Configuration)
        {
            _configuration = Configuration;

            // Dependency Injection issues with code base
            ITGDatabase ITGDatabaseCachedInstance = ITGDatabase.GetInstance();
            ITGDatabaseCachedInstance.AddConnectionString("JC2DataSource", _configuration.GetConnectionString("JC2DataSource"));
            ITGDatabaseCachedInstance.AddConnectionString("SessionStateDataSource", _configuration.GetConnectionString("SessionStateDataSource"));
        }
        //*********************************************************************************************


        [HttpPost]
        [ValidateModelState]
        [Route("api/IVR/1.0.0/GetCompanyByPhoneNumber")]
        [SwaggerOperation("GetCompanyByPhoneNumber")]
        [SwaggerResponse((int)HttpStatusCode.OK, "GetCompanyByPhoneNumber Response", typeof(ResponseGetCompany))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "Bad Request", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Unauthorized", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, "Internal Server Error", typeof(ResponseError))]
        public virtual IActionResult GetCompanyByPhoneNumber([FromBody]RequestGetCompanyByPhoneNumber body)
        {
            ResponseGetCompany response = new ResponseGetCompany();

            using (log4net.ThreadContext.Stacks["NDC"].Push("GetCompanyByPhoneNumber"))
            {
                try
                {
                    // ********************************************************************************************
                    // Authenticate
                    string username = body?.authentication?.username;
                    string password = body?.authentication?.password;
                    if (!Sessions.AuthenticateUser(username, password) || (!username.Equals("CiscoIVR", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return Get_Unauthorized_Error(string.Empty, "Either Username or Password is wrong.", "Either Username or Password is wrong. Username [" + username + "] Password [" + password + "]");
                    }

                    // Find globalCallID
                    string ExternalID = body?.applicant?.externalid;
                    if (!Guid.TryParse(ExternalID, out Guid globalCallID))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to extract External ID.", "External ID is missing or improperly formatted");
                    }


                    // ********************************************************************************************
                    int LaID = Validation.ConvertLanguageIDIntoLaID(body?.applicant?.languageid.ToString());


                    // Do we need phone number validation - standardization?
                    string PhoneNumber = body?.applicant?.phonenumber;
                    string ScrubbedPhoneNumber = string.Empty;
                    Validation.TryParsePhone(PhoneNumber, out ScrubbedPhoneNumber);


                    int CoID = 0;
                    bool CallIDAlreadyUsed = false;
                    if ((globalCallID != Guid.Empty) && (!string.IsNullOrWhiteSpace(ScrubbedPhoneNumber)))
                    {
                        CoID = ATSCisco.InitializeCiscoCall(globalCallID, ScrubbedPhoneNumber, LaID, out CallIDAlreadyUsed);
                    }

                    if (CallIDAlreadyUsed)
                    {
                        return Get_BadRequest_Error(string.Empty, "CallID already used.", "CallID [" + globalCallID.ToString() + "] already used.");
                    }

                    // ***********************************************************************************
                    if (CoID > 0)
                    {
                        // Status "GetCompanyByPhoneNumber - Company Found" should be set by insert

                        // Company found, start IVR
                        response.companyid = CoID;
                        response.action = ResponseGetCompanyActions.PlayCompanyRecording;
                        response.recordingfile = GetCompanyRecording(CoID, LaID);
                        response.redirecturl = string.Empty;
                    }
                    else
                    {
                        // Company not found, ask for location phone number
                        response.companyid = 0;
                        response.action = ResponseGetCompanyActions.AskOverrideCode;
                        response.recordingfile = string.Empty;
                        response.redirecturl = " "; // There is no ID to pass
                    }
                }
                catch (Exception ex)
                {
                    return Get_InternalServerError_Error(string.Empty, "Unknown error.  Ask First Advantage to check logs.", string.Empty, ex);
                }

                return new ObjectResult(response);
            }
        }


        [HttpPost]
        [ValidateModelState]
        [Route("api/IVR/1.0.0/GetCompanyByCode")]
        [SwaggerOperation("GetCompanyByCode")]
        [SwaggerResponse((int)HttpStatusCode.OK, "GetCompanyByCode Response", typeof(ResponseGetCompany))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "Bad Request", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Unauthorized", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, "Internal Server Error", typeof(ResponseError))]
        public virtual IActionResult GetCompanyByCode([FromBody]RequestGetCompanyByCode body)
        {
            ResponseGetCompany response = new ResponseGetCompany();

            using (log4net.ThreadContext.Stacks["NDC"].Push("GetCompanyByCode"))
            {
                try
                {
                    // ********************************************************************************************
                    // Authenticate
                    string username = body?.authentication?.username;
                    string password = body?.authentication?.password;
                    if (!Sessions.AuthenticateUser(username, password) || (!username.Equals("CiscoIVR", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return Get_Unauthorized_Error(string.Empty, "Either Username or Password is wrong.", "Either Username or Password is wrong. Username [" + username + "] Password [" + password + "]");
                    }

                    // Find globalCallID
                    string ExternalID = body?.applicant?.externalid;
                    if (!Guid.TryParse(ExternalID, out Guid globalCallID))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to extract External ID.", "External ID is missing or improperly formatted");
                    }


                    // ********************************************************************************************
                    int LaID = Validation.ConvertLanguageIDIntoLaID(body?.applicant?.languageid.ToString());


                    // Do we need phone number validation - standardization?
                    string PhoneNumber = body?.applicant?.phonenumber;
                    string ScrubbedPhoneNumber = string.Empty;
                    Validation.TryParsePhone(PhoneNumber, out ScrubbedPhoneNumber);

                    string OverrideCode = body?.applicant?.overridecode ?? string.Empty;
                    string ScrubbedOverridePhoneNumber = string.Empty;
                    Validation.TryParsePhone(OverrideCode, out ScrubbedOverridePhoneNumber);


                    int CoID = 0;
                    bool CallIDAlreadyUsed = false;
                    if ((globalCallID != Guid.Empty) && (!string.IsNullOrWhiteSpace(ScrubbedOverridePhoneNumber)))
                    {
                        CoID = ATSCisco.InitializeCiscoCall(globalCallID, ScrubbedOverridePhoneNumber, LaID, out CallIDAlreadyUsed);
                    }

                    if (CallIDAlreadyUsed)
                    {
                        return Get_BadRequest_Error(string.Empty, "CallID already used.", "CallID [" + globalCallID.ToString() + "] already used.");
                    }

                    // ***********************************************************************************
                    if (CoID > 0)
                    {
                        // Status "GetCompanyByPhoneNumber - Company Found" should be set by insert

                        // Company found, start IVR
                        response.companyid = CoID;
                        response.action = ResponseGetCompanyActions.PlayCompanyRecording;
                        response.recordingfile = GetCompanyRecording(CoID, LaID);
                        response.redirecturl = string.Empty;
                    }
                    else
                    {
                        string CloseRecording = string.Empty;
                        if (ATSCisco.IsCSRAvailable(out CloseRecording))
                        {
                            ATSCisco.UpdateCiscoCalls(globalCallID, 0, DateTime.Now, null, ATSCisco.CallStatusID.GetCompanyByPhoneNumber_No_Company_Found_CSR_Transfer);

                            // Company not found, start CSR
                            response.companyid = 0;
                            response.action = ResponseGetCompanyActions.RedirectToCSR;
                            response.recordingfile = string.Empty;
                            response.redirecturl = " "; // There is no ID to pass
                                                        // Return URL - CsrUrlBase + @"JobCredits/Questionnaire/CSR/LocationSearch.aspx";
                        }
                        else
                        {
                            ATSCisco.UpdateCiscoCalls(globalCallID, 0, DateTime.Now, null, ATSCisco.CallStatusID.GetCompanyByPhoneNumber_No_Company_Found_No_CSR_Call_Hang_Up);

                            // Company not found, no CSR; play close message
                            response.companyid = 0;
                            response.action = ResponseGetCompanyActions.PlayCloseRecording;
                            response.recordingfile = GetCloseMessageRecording(CloseRecording, LaID);
                            response.redirecturl = string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Get_InternalServerError_Error(string.Empty, "Unknown error.  Ask First Advantage to check logs.", string.Empty, ex);
                }

                return new ObjectResult(response);
            }
        }


        [HttpPost]
        [ValidateModelState]
        [Route("api/IVR/1.0.0/NewApplicant")]
        [SwaggerOperation("NewApplicant")]
        [SwaggerResponse((int)HttpStatusCode.OK, "NewApplicant Response", typeof(ResponseNewApplicant))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "Bad Request", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Unauthorized", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, "Internal Server Error", typeof(ResponseError))]
        public virtual IActionResult NewApplicant([FromBody]RequestNewApplicant body)
        {
            ResponseNewApplicant response = new ResponseNewApplicant();

            using (log4net.ThreadContext.Stacks["NDC"].Push("NewApplicant"))
            {
                try
                {
                    // ********************************************************************************************
                    // Authenticate
                    string username = body?.authentication?.username;
                    string password = body?.authentication?.password;
                    if (!Sessions.AuthenticateUser(username, password) || (!username.Equals("CiscoIVR", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return Get_Unauthorized_Error(string.Empty, "Either Username or Password is wrong.", "Either Username or Password is wrong. Username [" + username + "] Password [" + password + "]");
                    }

                    // Find globalCallID
                    string ExternalID = body?.applicant?.externalid;
                    if (!Guid.TryParse(ExternalID, out Guid globalCallID))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to extract External ID.", "External ID is missing or improperly formatted");
                    }

                    // Verify globalCallID
                    if (!ATSCisco.LookupCiscoCalls(globalCallID, out int LaID, out int CoID, out int LoID, out int ClID, out int EmID, out string EmployerLocationCode, out Guid SessionGuid, out ATSCisco.CallStatusID CallStatus))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to find call information.", "External ID used is [" + ExternalID + "]");
                    }


                    // ********************************************************************************************
                    // City and State search by Zip Code
                    if (!Validation.TryParseZip(body?.applicant?.zip, out string ApplicantZip))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to extract Zip Code.", "Zip Code is missing or improperly formatted");
                    }

                    ATSCisco.LookupCityState(globalCallID, ApplicantZip, out string ApplicantCity, out string ApplicantStateCode);


                    // ********************************************************************************************
                    // Now create an applicant object.
                    Validation.TryParseSSN(body?.applicant?.ssn, out string SSN);
                    Validation.TryParseBirthDate(body?.applicant?.birthdate.ToString(), out DateTime BirthDate);


                    // ******************************************************************************************
                    //  Create an EmID and EmployeeSessions row in database, if needed
                    // ******************************************************************************************
                    Screening.PrepareScreening
                    (
                        CoID,
                        username,
                        "WebAPI.IVR",

                        0,                      //EmID,
                        SSN,
                        globalCallID.ToString(),//ExternalID,
                        51,                     // AtsID for CiscoIVR

                        string.Empty,           // FirstName,
                        string.Empty,           // MiddleInitial,
                        string.Empty,           // LastName,
                        BirthDate,

                        string.Empty,           // EmailAddress,
                        string.Empty,           // PhoneNumber,

                        string.Empty,           // ApplicantStreet,
                        ApplicantCity,
                        ApplicantStateCode,
                        ApplicantZip,

                        EmployerLocationCode,
                        string.Empty,           //Position,
                        0.0m,                   //PayRate,

                        Guid.Empty,

                        out int FinalEmID,
                        out Guid PendingSessionGuid,
                        out bool ShouldRescreen
                    );


                    // ******************************************************************************************
                    //  Set up sessions
                    // ******************************************************************************************
                    SessionItems si = new SessionItems
                    (
                       PendingSessionGuid,
                       CoID,
                       FinalEmID,
                       SSN,
                       1, // LaID
                       string.Empty, // FName
                       string.Empty, // Mi
                       string.Empty, // LName
                       DateTime.MinValue, // DOB
                       string.Empty, // Email
                       string.Empty, // Street
                       ApplicantCity,
                       ApplicantStateCode,
                       ApplicantZip,
                       string.Empty, // Country
                       EmployerLocationCode,
                       string.Empty, // Position
                       string.Empty, // Redirect URL
                       string.Empty, // Request ID
                       "CSRIVR", // Result format
                       globalCallID.ToString(), //ExternalID,
                       string.Empty, // Postback URL
                       string.Empty, // Transact ID
                       string.Empty // Requisition ID
                    );


                    // ******************************************************************************************
                    // Check to see if we found the applicant.
                    Applicant applicant = new Applicant(FinalEmID);
                    if (applicant.EmID <= 0)
                    {
                        return Get_Unauthorized_Error(string.Empty, "Unable to match external id to an employee.", "External ID used is [" + ExternalID + "]");
                    }


                    // Update tracking table
                    SessionGuid = PendingSessionGuid;
                    ATSCisco.UpdateCiscoCalls(globalCallID, FinalEmID, null, SessionGuid, ATSCisco.CallStatusID.PostAnswers_Collecting_GetDigits_Questions);


                    // ***********************************************************************************************
                    // Save answers

                    // If Under 40 sent as true, save question
                    if ((body?.applicant?.under40).HasValue)
                    {
                        List<Answer> answers = new List<Answer>();

                        int UnderFortyQuestionID = 1000;
                        bool UnderFortyAnswer = body?.applicant?.under40 ?? false;

                        Answer underForty = new Answer(UnderFortyAnswer.ToString(), FinalEmID, UnderFortyQuestionID);
                        answers.Add(underForty);

                        Answers.SaveAnswers(answers, out bool IsAllAnswersSaved);
                    }


                    // ***********************************************************************************************

                    // Initial Page [1, 101 implied]
                    Screening.UpdateScreeningProgress(applicant.EmID, SessionGuid, QuestionaireStatus.IntroPageCompleted, string.Empty, false, DateTime.MinValue);


                    // ***********************************************************************************************
                    // Get Common Response
                    int PostedQuestionID = 0;
                    bool IsPostedAnswerSaved = false;
                    response.applicant = Get_ResponseNewApplicantApplicant(globalCallID, applicant, SessionGuid, LaID, PostedQuestionID, IsPostedAnswerSaved, ATSCisco.CallStatusID.PostAnswers_Collecting_GetDigits_Questions);
                }
                catch (Exception ex)
                {
                    return Get_InternalServerError_Error(string.Empty, "Unknown error.  Ask First Advantage to check logs.", string.Empty, ex);
                }
            }

            return new ObjectResult(response);
        }


        [HttpPost]
        [ValidateModelState]
        [Route("api/IVR/1.0.0/PostAnswers")]
        [SwaggerOperation("PostAnswers")]
        [SwaggerResponse((int)HttpStatusCode.OK, "PostAnswers Response", typeof(ResponsePostAnswers))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "Bad Request", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.Unauthorized, "Unauthorized", typeof(ResponseError))]
        [SwaggerResponse((int)HttpStatusCode.InternalServerError, "Internal Server Error", typeof(ResponseError))]
        public virtual IActionResult PostAnswers([FromBody]RequestPostAnswers body)
        {
            ResponsePostAnswers response = new ResponsePostAnswers();

            using (log4net.ThreadContext.Stacks["NDC"].Push("PostAnswers"))
            {
                try
                {
                    // ********************************************************************************************
                    // Authenticate
                    string username = body?.authentication?.username;
                    string password = body?.authentication?.password;
                    if (!Sessions.AuthenticateUser(username, password) || (!username.Equals("CiscoIVR", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return Get_Unauthorized_Error(string.Empty, "Either Username or Password is wrong.", "Either Username or Password is wrong. Username [" + username + "] Password [" + password + "]");
                    }

                    // Find globalCallID
                    string ExternalID = body?.applicant?.externalid;
                    if (!Guid.TryParse(ExternalID, out Guid globalCallID))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to extract External ID.", "External ID is missing or improperly formatted");
                    }

                    // Verify globalCallID
                    if (!ATSCisco.LookupCiscoCalls(globalCallID, out int LaID, out int CoID, out int LoID, out int ClID, out int EmID, out string EmployerLocationCode, out Guid SessionGuid, out ATSCisco.CallStatusID CallStatus))
                    {
                        return Get_BadRequest_Error(string.Empty, "Unable to find call information.", "External ID used is [" + ExternalID + "]");
                    }


                    // ********************************************************************************************
                    // Check to see if we found the applicant.
                    Applicant applicant = new Applicant(EmID);
                    if (applicant.EmID <= 0)
                    {
                        return Get_Unauthorized_Error(string.Empty, "Unable to match external id to an employee.", "External ID used is [" + ExternalID + "]");
                    }


                    // ***********************************************************************************************
                    // Save answers
                    int requestQuestionId = 0;
                    string requestAnswerText = string.Empty;
                    foreach (PostAnswersAnswer requestAnswer in body?.applicant?.answers)
                    {
                        requestQuestionId = requestAnswer.questionid;
                        requestAnswerText = requestAnswer.value;
                    }

                    // Track progress, Questions answered [2]
                    Screening.UpdateScreeningProgress(applicant.EmID, SessionGuid, QuestionaireStatus.InitialQuestionPageCompleted, requestQuestionId.ToString(), false, DateTime.MinValue);



                    bool IsPostedAnswerSaved = false;

                    // Detect recording...
                    Questionnaire.QuestionnaireQuestionTypes currentQuestType
                        = Questionnaire.Instance.AllQuestions[requestQuestionId].QuestionType;

                    if
                    (
                        (
                            currentQuestType == Questionnaire.QuestionnaireQuestionTypes.Integer
                            || currentQuestType == Questionnaire.QuestionnaireQuestionTypes.Text
                            || currentQuestType == Questionnaire.QuestionnaireQuestionTypes.Money
                            || currentQuestType == Questionnaire.QuestionnaireQuestionTypes.Phone
                            || currentQuestType == Questionnaire.QuestionnaireQuestionTypes.Email
                            || currentQuestType == Questionnaire.QuestionnaireQuestionTypes.CityState
                            || currentQuestType == Questionnaire.QuestionnaireQuestionTypes.State
                        )
                        && Questionnaire.Instance.AllQuestions[requestQuestionId].QuestionID != 95
                    )
                    {
                        string recordingFileName = GetAnswerRecordingFileName(EmID, requestQuestionId);
                        ATSCisco.SaveRecording(EmID, globalCallID.ToString(), requestQuestionId, recordingFileName);

                        IsPostedAnswerSaved = true;
                    }
                    else
                    {
                        // Map answers from IVR to standard ex. 9 = 0 (no)
                        Answer jcAnswer = ATSCisco.MapIVRAnswers(new Answer(requestAnswerText, EmID, requestQuestionId), applicant);

                        if (jcAnswer != null)
                        {
                            // Post the answers, should also refresh the questionaire with new questions
                            List<Answer> answers = new List<Answer>()
                            {
                               jcAnswer
                            };

                            HashSet<int> QuestionsWithAnswersSaved = Answers.SaveAnswers(answers, out bool IsAllAnswersSaved);
                            IsPostedAnswerSaved = (QuestionsWithAnswersSaved.Count > 0);
                        }
                    }


                    // ***********************************************************************************************
                    // This is a temp fix. Waiting on CSR page to have the update added
                    // This should only be called if not qualified.
                    if (requestQuestionId == 95)
                    {
                        Screening.UpdateScreeningProgress(EmID, SessionGuid, QuestionaireStatus.ESignCollected, string.Empty, false, DateTime.MinValue);


                        // ***********************************************************************************************
                        // Hire check 
                        ScreeningConfig sc = new ScreeningConfig(CoID);
                        if (sc.IVRAutoHireScreens)
                        {
                            try
                            {
                                DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                                if (!Applicant.AddEmployeeHireDate(null, applicant.EmSSN, CoID, applicant.LoID, startDate, DateTime.MinValue,
                                    applicant.EmFName, applicant.EmLName, applicant.EmHourlyRate, applicant.EmPosition, applicant.EmID, out int HireOutputEmID,
                                    applicant?.ApplicantAddress?.Street, applicant?.ApplicantAddress?.City, applicant?.ApplicantAddress?.State, applicant?.ApplicantAddress?.Zip,
                                     applicant.ClID, 11, applicant.EmBirthDate, "CiscoIVR"))
                                {
                                    log.Error($"Hire Fail EmID [{applicant.EmID}]");
                                }
                            }
                            catch (Exception hireException)
                            {
                                log.Error($"Hire Fail EmID [{applicant.EmID}]", hireException);
                            }
                        }
                    }
                    // ***********************************************************************************************


                    // ***********************************************************************************************
                    // Get Common Response
                    // This is needed to identify if optional questions were asked


                    response.applicant = Get_ResponseNewApplicantApplicant(globalCallID, applicant, SessionGuid, LaID, requestQuestionId, IsPostedAnswerSaved, CallStatus);
                }
                catch (Exception ex)
                {
                    return Get_InternalServerError_Error(string.Empty, "Unknown error.  Ask First Advantage to check logs.", string.Empty, ex);
                }
            }

            return new ObjectResult(response);
        }


        public ResponseNewApplicantApplicant Get_ResponseNewApplicantApplicant(Guid ExternalID, Applicant applicant, Guid SessionGuid, int LaID, int PostedQuestionID, bool IsPostedAnswerSaved, ATSCisco.CallStatusID CallStatus)
        {
            ResponseNewApplicantApplicant responseApplicant = new ResponseNewApplicantApplicant
            {
                applicantid = applicant.EmID
            };


            // ***********************************************************************************************
            // Check for IVR Question
            if (CallStatus == ATSCisco.CallStatusID.PostAnswers_Collecting_GetDigits_Questions)
            {
                responseApplicant.question = GetIVRQuestion(applicant.CoID, applicant.EmID, LaID, PostedQuestionID, IsPostedAnswerSaved);
                if (responseApplicant.question == null)
                {
                    // Calculate on IVR answers
                    bool IsQualified = false;
                    if (responseApplicant.question == null)
                    {
                        bool ReCalculate = true;
                        Credits.RecalulateCreditsToReturnExternalEligiblity(applicant.EmID, ReCalculate, out bool FederalQualified, out bool StateQualified, out List<Credit> CreditsList);
                        IsQualified = (FederalQualified || StateQualified);
                    }


                    // If qualified, do form fill or CSR transfer or hangup
                    if (IsQualified)
                    {
                        // Check CSR hours
                        string CloseRecording = string.Empty;
                        if (ATSCisco.IsCSRAvailable(out CloseRecording))
                        {
                            // Continue Call if possible
                            ATSCisco.UpdateCiscoCalls(ExternalID, applicant.EmID, DateTime.Now, SessionGuid, ATSCisco.CallStatusID.PostAnswers_Credit_Qualified_CSR_Transfer);

                            responseApplicant.action = IVRApplicantActions.RedirectToCSR;
                            responseApplicant.recordingfile = string.Empty;
                            responseApplicant.redirecturl = SessionGuid.ToString();
                            responseApplicant.registrationnumber = Com.Itg.JobCredits.Business.CSR.LookUpIVRRegistrationCode("CiscoIVR", applicant.EmID);
                            //CsrUrlBase + @"JobCredits/Questionnaire/csr/ApplicantInfoIVR.aspx?Src=IvrTransfer&arg=" +
                        }
                        else
                        {
                            // Start the form fill
                            CallStatus = ATSCisco.CallStatusID.PostAnswers_Collecting_GetRecording_PII_Questions;
                            ATSCisco.UpdateCiscoCalls(ExternalID, applicant.EmID, null, SessionGuid, ATSCisco.CallStatusID.PostAnswers_Collecting_GetRecording_PII_Questions);

                            // Reset the prior answer object, only on the status transition
                            PostedQuestionID = 0;
                        }
                    }
                    else
                    {
                        // End Call
                        ATSCisco.UpdateCiscoCalls(ExternalID, applicant.EmID, DateTime.Now, SessionGuid, ATSCisco.CallStatusID.PostAnswers_Credit_Not_Qualified_Call_Hang_Up);

                        responseApplicant.action = IVRApplicantActions.PlayThanksRecording;
                        responseApplicant.recordingfile = GetThanksRecording(LaID, applicant.CoID, applicant.EmID);
                        responseApplicant.redirecturl = string.Empty;
                        responseApplicant.registrationnumber = Com.Itg.JobCredits.Business.CSR.LookUpIVRRegistrationCode("CiscoIVR", applicant.EmID);
                    }
                }
            }


            // ***********************************************************************************************
            // Check for PII FormFill Question
            if (CallStatus == ATSCisco.CallStatusID.PostAnswers_Collecting_GetRecording_PII_Questions)
            {
                responseApplicant.question = GetNoCSRPIIQuestion(applicant.CoID, applicant.EmID, LaID, PostedQuestionID);
                if (responseApplicant.question == null)
                {
                    CallStatus = ATSCisco.CallStatusID.PostAnswers_Collecting_GetRecording_FormFill_Questions;
                    ATSCisco.UpdateCiscoCalls(ExternalID, applicant.EmID, null, SessionGuid, ATSCisco.CallStatusID.PostAnswers_Collecting_GetRecording_FormFill_Questions);
                }
            }


            // ***********************************************************************************************
            // Check for FormFill Question
            if (CallStatus == ATSCisco.CallStatusID.PostAnswers_Collecting_GetRecording_FormFill_Questions)
            {
                // Get the form fill questions
                responseApplicant.question = GetNoCSRQuestion(applicant.CoID, applicant.EmID, LaID, PostedQuestionID);


                // Add end of form fill.. add Esign question for recording
                //if ((responseApplicant.question == null) && (!QuestionsAlreadyAsked.Contains(95)))
                //{
                //    responseApplicant.question = new Models.V1.Question
                //    {
                //        questionid = 95,
                //        answertype = IVRQuestionnaireQuestionTypes.GetRecording,
                //        answersize = 0,
                //        questiontext = Questionnaire.Instance.AllQuestions[95].QuestionLanguages[(WebSiteLabels.LanguageIDs)LaID].QuestionText,
                //        questionrecording = GetQuestionRecording(95, LaID),
                //        instructionrecording = GetQuestionTypeRecording(Questionnaire.Instance.AllQuestions[95].QuestionType, LaID),
                //        validationrecording = GetValidationErrorRecording(95, QuestionsAlreadyAsked, LaID),
                //        answerrecordingfilename = GetAnswerRecordingFileName(applicant.EmID, 95)
                //    };
                //}


                if (responseApplicant.question == null)
                {
                    // Form Fill completed
                    ATSCisco.UpdateCiscoCalls(ExternalID, applicant.EmID, DateTime.Now, SessionGuid, ATSCisco.CallStatusID.PostAnswers_Credit_Qualified_FormFill_Done_Call_Hang_Up);

                    responseApplicant.action = IVRApplicantActions.PlayThanksRecording;
                    responseApplicant.recordingfile = GetThanksRecording(LaID, applicant.CoID, applicant.EmID);
                    responseApplicant.redirecturl = string.Empty;
                    responseApplicant.registrationnumber = Com.Itg.JobCredits.Business.CSR.LookUpIVRRegistrationCode("CiscoIVR", applicant.EmID);


                    // All questions answered
                    Screening.UpdateScreeningProgress(applicant.EmID, SessionGuid, QuestionaireStatus.ESignCollected, string.Empty, false, DateTime.MinValue);
                }
            }


            // ***********************************************************************
            // File status clean up if there is a question to ask
            if (responseApplicant.question != null)
            {
                // More questions to ask
                responseApplicant.action = IVRApplicantActions.AskQuestions;
                responseApplicant.recordingfile = string.Empty;
                responseApplicant.redirecturl = string.Empty;
                responseApplicant.registrationnumber = string.Empty;


                // Update the status of this screening
                string StatusDetails = responseApplicant.question.questionid.ToString();

                // Questions returned [102]
                Screening.UpdateScreeningProgress(applicant.EmID, SessionGuid, QuestionaireStatus.InitialQuestionPageDisplayed, StatusDetails, false, DateTime.MinValue);
            }


            return responseApplicant;
        }


        // *************************************************************************************************************************************************
        // Other logic
        // *************************************************************************************************************************************************

        private static Models.V1.Question GetIVRQuestion(int CoID, int EmID, int LaID, int PostedQuestionID, bool IsPostedAnswerSaved)
        {
            Models.V1.Question ivrQuestion = null;

            HashSet<int> QuestionsAlreadyAsked = new HashSet<int>();
            if (IsPostedAnswerSaved)
            {
                QuestionsAlreadyAsked.Add(PostedQuestionID);
            }

            ScreeningConfig sc = new ScreeningConfig(CoID);

            // Override the company setting.  IVR only has recordings for New Questionnaire
            sc.QuestionSet_Type = QuestionSetType.NewQuestionSet;

            List<Com.Itg.JobCredits.Business.Question> QuestionList = Questions.GetNextUnansweredQuestions(EmID, LaID, sc, QuestionsAlreadyAsked);


            List<Answer> tempAnswers = new List<Answer>();

            IVRQuestionnaireQuestionTypes answerType = IVRQuestionnaireQuestionTypes.GetDigits;
            int answerSize = 50;

            foreach (Com.Itg.JobCredits.Business.Question q in QuestionList)
            {
                switch (q.QuestionType)
                {
                    case Questionnaire.QuestionnaireQuestionTypes.YesNo:
                        answerType = IVRQuestionnaireQuestionTypes.GetDigits;
                        answerSize = 1;
                        break;

                    // This shouldn't be asked in IVR (unknown number of digits)
                    case Questionnaire.QuestionnaireQuestionTypes.Integer:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;

                    case Questionnaire.QuestionnaireQuestionTypes.Text:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;

                    // Wav field should ask yyyymmdd
                    case Questionnaire.QuestionnaireQuestionTypes.Date:
                        answerType = IVRQuestionnaireQuestionTypes.GetDigits;
                        answerSize = 8;
                        break;

                    // This shouldn't be asked in IVR (unknown number of digits)
                    case Questionnaire.QuestionnaireQuestionTypes.Money:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;

                    // This shouldn't be asked in IVR
                    case Questionnaire.QuestionnaireQuestionTypes.Phone:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;

                    // This shouldn't be asked in IVR
                    case Questionnaire.QuestionnaireQuestionTypes.Email:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;

                    // Wav field should list option 1-4
                    case Questionnaire.QuestionnaireQuestionTypes.BranchOfService:
                        answerType = IVRQuestionnaireQuestionTypes.GetDigits;
                        answerSize = 1;
                        break;

                    // This shouldn't be asked in IVR
                    case Questionnaire.QuestionnaireQuestionTypes.CityState:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;
                    case Questionnaire.QuestionnaireQuestionTypes.YesNoMaybe:
                        answerType = IVRQuestionnaireQuestionTypes.GetDigits;
                        answerSize = 1;
                        break;

                    // This shouldn't be asked in IVR
                    case Questionnaire.QuestionnaireQuestionTypes.State:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;
                    default:
                        answerType = IVRQuestionnaireQuestionTypes.GetRecording;
                        answerSize = 0;
                        break;
                }


                if (answerType == IVRQuestionnaireQuestionTypes.GetDigits)
                {
                    if (ivrQuestion == null)
                    {
                        ivrQuestion = new Models.V1.Question
                        {
                            questionid = q.QuestionID,
                            answertype = answerType,
                            answersize = answerSize,
                            questiontext = q.QuestionLanguages[(WebSiteLabels.LanguageIDs)LaID].QuestionText,
                            questionrecording = GetQuestionRecording(q.QuestionID, LaID),
                            instructionrecording = GetInstructionRecording(q.QuestionID, q.QuestionType, LaID),
                            validationrecording = GetValidationErrorRecording(q.QuestionID, PostedQuestionID, LaID),
                            answerrecordingfilename = string.Empty
                        };
                    }
                }
                else
                {
                    // Needs_CSR_Or_IVR_Recording
                    if (q.QuestionID != 95)
                    {
                        // Esign won't save blanks
                        tempAnswers.Add(new Answer(string.Empty, EmID, q.QuestionID));
                    }
                    else
                    {
                        ivrQuestion = new Models.V1.Question
                        {
                            questionid = q.QuestionID,
                            answertype = IVRQuestionnaireQuestionTypes.GetDigits,
                            answersize = 1,
                            questiontext = q.QuestionLanguages[(WebSiteLabels.LanguageIDs)LaID].QuestionText,
                            questionrecording = GetQuestionRecording(q.QuestionID, LaID),
                            instructionrecording = GetInstructionRecording(q.QuestionID, q.QuestionType, LaID),
                            validationrecording = GetValidationErrorRecording(q.QuestionID, PostedQuestionID, LaID),
                            answerrecordingfilename = string.Empty
                        };
                    }
                }
            }

            // Auto-Save blank answers for all recording questions
            if (tempAnswers.Count > 0)
            {
                Answers.SaveAnswers(tempAnswers, out bool IsAllAnswersSaved);
            }


            // Check for Page Transition
            if ((ivrQuestion == null) && (tempAnswers.Count > 0))
            {
                PostedQuestionID = 0;
                IsPostedAnswerSaved = false;
                ivrQuestion = GetIVRQuestion(CoID, EmID, LaID, PostedQuestionID, IsPostedAnswerSaved);
            }


            return ivrQuestion;
        }


        private static Models.V1.Question GetNoCSRPIIQuestion(int CoID, int EmID, int LaID, int PostedQuestionID)
        {
            Models.V1.Question question = null;

            // Do the fake questions in order
            int NextQuestionID = 0;
            if (PostedQuestionID == 0)
            {
                //QuID	QuestionText
                //3000    First Name
                //3001    Middle Name
                //3002    Last Name
                //3003    Address
                //3004    City
                //3005    State
                NextQuestionID = 3000;
            }
            else
            {
                if ((3000 <= PostedQuestionID) && (PostedQuestionID <= 3004))
                {
                    NextQuestionID = PostedQuestionID + 1;
                }
            }


            if (NextQuestionID > 0)
            {
                question = new Models.V1.Question
                {
                    questionid = NextQuestionID,
                    answertype = IVRQuestionnaireQuestionTypes.GetRecording,
                    answersize = 0,
                    questiontext = Questionnaire.Instance.AllQuestions[NextQuestionID].QuestionLanguages[(WebSiteLabels.LanguageIDs)LaID].QuestionText,
                    questionrecording = GetQuestionRecording(NextQuestionID, LaID),
                    instructionrecording = GetInstructionRecording(NextQuestionID, Questionnaire.Instance.AllQuestions[NextQuestionID].QuestionType, LaID),
                    validationrecording = GetValidationErrorRecording(NextQuestionID, PostedQuestionID, LaID),
                    answerrecordingfilename = GetAnswerRecordingFileName(EmID, NextQuestionID)
                };
            }

            return question;
        }


        private static Models.V1.Question GetNoCSRQuestion(int CoID, int EmID, int LaID, int PostedQuestionID)
        {
            Models.V1.Question question = null;


            // Find next blank document answer
            int NextQuestionID = ATSCisco.FindNextFormFillQuestion(EmID);


            if (NextQuestionID > 0)
            {
                question = new Models.V1.Question
                {
                    questionid = NextQuestionID,
                    answertype = IVRQuestionnaireQuestionTypes.GetRecording,
                    answersize = 0,
                    questiontext = Questionnaire.Instance.AllQuestions[NextQuestionID].QuestionLanguages[(WebSiteLabels.LanguageIDs)LaID].QuestionText,
                    questionrecording = GetQuestionRecording(NextQuestionID, LaID),
                    instructionrecording = GetInstructionRecording(NextQuestionID, Questionnaire.Instance.AllQuestions[NextQuestionID].QuestionType, LaID),
                    validationrecording = GetValidationErrorRecording(NextQuestionID, PostedQuestionID, LaID),
                    answerrecordingfilename = GetAnswerRecordingFileName(EmID, NextQuestionID)
                };
            }

            return question;
        }


        private static string GetUrlBase()
        {
            string UrlBase = @"https://www.OBSCURED.com/";

            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env == "PROD")
            {
                UrlBase = @"https://www.OBSCURED.com/";
            }
            else if (env == "UAT")
            {
                UrlBase = @"https://test.OBSCURED.com/";
            }
            else if (env == "QA")
            {
                UrlBase = @"https://qajobcredits.OBSCURED.com/";
            }
            else //if (env.IsEnvironment("LOCAL") || env.IsEnvironment("DEV"))
            {
                UrlBase = @"http://localhost:29175/";
            }



            return UrlBase;
        }


        private static string GetAnswerRecordingFileName(int EmID, int QuestionID)
        {
            return "EmID_" + EmID.ToString() + "_QuestionID_" + QuestionID.ToString() + ".wav";
        }




        // *************************************************************************************************************************************************
        // Recording Versioning
        // Todo: Check for file existence and check for versioning
        // *************************************************************************************************************************************************

        #region Recordings

        private static string GetCompanyRecording(int CoID, int LaID)
        {
            return "CoID_" + CoID.ToString() + (LaID == 1 ? "" : "_ES") + ".wav";
        }


        private static string GetCloseMessageRecording(string CloseRecording, int LaID)
        {
            string retFileName = CloseRecording;
            // Make sure language part of file name matches LaID
            if (LaID == 1)
            {
                retFileName = retFileName.Replace("ES", "EN");
            }
            else
            {
                retFileName = retFileName.Replace("EN", "ES");
            }

            return retFileName;
        }


        private static string GetQuestionRecording(int QuestionID, int LaID)
        {
            string languageNote = "EN";
            if (LaID == 2)
            {
                languageNote = "ES";
            }

            return "QuID_" + QuestionID.ToString() + "_" + languageNote + ".wav"; ;
        }


        private static string GetInstructionRecording(int QuestionID, Questionnaire.QuestionnaireQuestionTypes questionType, int LaID)
        {
            string languageNote = "EN";
            if (LaID == 2)
            {
                languageNote = "ES";
            }

            /*
                QuestionID	    QuestionText
                13	            What is your Parole Officer's phone number?
                473	            If applicable, what is your maiden name?
                1004	        Unemployment start date?
             */
            if (QuestionID == 13 || QuestionID == 473 || QuestionID == 1004)
            {
                return "Instruction_QuID_" + QuestionID.ToString() + "_" + languageNote + ".wav";
            }
            else
            {
                return questionType.ToString() + "_" + languageNote + ".wav";
            }
        }

        private static string GetValidationErrorRecording(int QuestionID, int PostedQuestionID, int LaID)
        {
            string recording = string.Empty;

            // Question is being asked twice...
            if (QuestionID == PostedQuestionID)
            {
                string languageNote = "EN";
                if (LaID == 2)
                {
                    languageNote = "ES";
                }


                recording = "AnswerValidationError_" + languageNote + ".wav";
            }

            return recording;
        }



        private static string GetThanksRecording(int LaID, int CoID, int EmID)
        {
            string languageNote = "EN";
            if (LaID == 2)
            {
                languageNote = "ES";
            }

            string ThanksWavFileName = "Thanks_" + languageNote + ".wav";


            return ThanksWavFileName;
        }



        #endregion


        // *************************************************************************************************************************************************
        // Error logic
        // *************************************************************************************************************************************************

        #region Bad Response

        private ObjectResult Get_BadRequest_Error(string ErrorCode, string ErrorMessage, string InternalLogMessage)
        {
            log.Error(InternalLogMessage);

            ResponseError fadvError = new ResponseError
            {
                code = ErrorCode,
                message = ErrorMessage
            };

            return StatusCode((int)HttpStatusCode.BadRequest, fadvError);
        }

        private ObjectResult Get_Unauthorized_Error(string ErrorCode, string ErrorMessage, string InternalLogMessage)
        {
            log.Error(InternalLogMessage);

            ResponseError fadvError = new ResponseError
            {
                code = ErrorCode,
                message = ErrorMessage
            };

            return StatusCode((int)HttpStatusCode.Unauthorized, fadvError);
        }

        private ObjectResult Get_InternalServerError_Error(string ErrorCode, string ErrorMessage, string InternalLogMessage)
        {
            log.Error(InternalLogMessage);

            ResponseError fadvError = new ResponseError
            {
                code = ErrorCode,
                message = ErrorMessage
            };

            return StatusCode((int)HttpStatusCode.InternalServerError, fadvError);
        }

        private ObjectResult Get_InternalServerError_Error(string ErrorCode, string ErrorMessage, string InternalLogMessage, Exception InternalLogException)
        {
            log.Error(InternalLogMessage, InternalLogException);

            ResponseError fadvError = new ResponseError
            {
                code = ErrorCode,
                message = ErrorMessage
            };

            return StatusCode((int)HttpStatusCode.InternalServerError, fadvError);
        }

        #endregion
    }
}