using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infinedi.Domain.ViewModels;
using Infinedi.Domain.Models.Claims_Processing;
using System.Data.SqlClient;
using System.Data;
using System.Dynamic;

namespace ClaimProcessingRepository.SQL
{
    //Defines the hierarchy of search when multiple search parameters are input for searching claims
    public static class ClaimsSearchOrchestration
    {
        public static List<string> s_columnNamesToRetrieve = new List<string>() { "Tracenumber" };
        public static string s_tableName = "Results";
        /** 
        1. Available params from Quick Links - Infinedi processing status
            a. AcceptedClaims for last 30 days
            b. ErroredClaims for last 30 days
            c. Failed claims for last 30 days
        2. Search from InputFiles Report status
            a. Clean for InputFileId
            b. Errored for InputFileId
            c. Rejected for InputFileId
        3. Search from ClaimProcessing
            a. DateFrom and DateTo
            b. TraceNumber
            c. Payer name
            d. Patient Last Name
            e. Patient First Name
            f. Patient Account Number 
        4. Search by webstatus
            a. Archived
            b. Pending
            c. Viewed
            d. On Hold
            e. Ignore
        5. Search by ClaimTypes - Inst, Prof, Dental
        6. Search by Workercomp type
        7. Pagenation
            a. Current Page Number 
            b. Rows Per Page 


        Every search needs to have one of these requirements:
        1. ProcessedDateFrom and ProcesseddateTo
        2. InputFileId, ProcesseddateFrom and ProcessedDateTo
        3. (Accepted or Errored or Rejected) and  (ProcessedDateFrom and ProcessedDateTo)
        The responsibility of this class is to route the search into initial steps depending on the sets of input params filled.     
        Priority 1: TraceNumber is not null
        Priority 2: InputFileId not null
        Priority 3: ProcessedDateFrom and ProcessedDateTo are not null
        Priority 4: Patient First, Last and/or account number
        Priority 5: Payer Name not null
        Priority 6: Accepted or Errored or Failed - Processing status or quick action
        Priority 7: WEbstatus
        Priority 8: Pagenation
    **/


        public static List<string> GetTraceNumbersForSearch(ClaimsProcessingViewModel vm, string clientCodeString)
        {
            List<string> _traceNumbersList = new List<string>();
            string _clientCodeStringForAdoQUery = "'" + clientCodeString.Replace(",", "','") + "'";
            string _undecorated_CommaSeparated_ClientCodes = clientCodeString.Replace("'", "");
            DataTable _userTable = new DataTable();
            SqlParameter _inputTraceNumbersFromDatesSearch = new SqlParameter();

            try
            {
                //Start by Date fields that are required and then filter it down per criteria
                if (vm.InputProcessedDateFrom != null && vm.InputProcessedDateTo != null)
                {
                  //  if (vm.InputProcessedDateFrom == vm.InputProcessedDateTo && vm.InputProcessedDateFrom != null)
                    {
                        vm.InputProcessedDateTo = ((DateTime)vm.InputProcessedDateTo).AddHours(23);
                    }
                    // _traceNumbersList is 309 at this point
                    _traceNumbersList = SearchByProcessedDates(_clientCodeStringForAdoQUery, vm.InputProcessedDateFrom, vm.InputProcessedDateTo);

                    _userTable.Columns.Add("Tracenumber", typeof(string));
                    foreach (var _trace in _traceNumbersList)
                    {
                        _userTable.Rows.Add(_trace);
                    }
                    _inputTraceNumbersFromDatesSearch = new SqlParameter("InputTraceNumbers", _userTable);


                }
                if (vm.InputTraceNumber != null)
                {
                    List<string> _interimResults = new List<string>();
                    _interimResults.Add(vm.InputTraceNumber); 
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }
                if (vm.InputInputFileID != null)
                {
                    List<string> _interimResults = new List<string>();
                    //GetAllTraceNumbersinInputFileId ;
                    _interimResults = SearchByInputFileId(vm, _clientCodeStringForAdoQUery);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }
                #region
                ////Search bY infinedi processing status
                //if (vm.SearchFromQuickActionAccepted == true)
                //{
                //    List<string> _interimResults = new List<string>();
                //    string query = @"SELECT distinct c.TraceNumber as trace from Claim c 
                //                    where
                //                     c.ClientCode in (" + _clientCodeStringForAdoQUery + ") " +
                //          "and c.ClaimStatusId < 9000" +
                //          "and c.TraceNumber is not null  and  c.ReceivedDate >= '" + vm.InputProcessedDateFrom + "' " +
                //          "and c.ReceivedDate <= '" + vm.InputProcessedDateTo + "'";
                //    DataSet ds = new DataSet();
                //    ds = DBFactories.DBFactory.DbCommandSelect("InfClaims", query, System.Data.CommandType.Text, null);
                //    foreach (DataRow dr in ds.Tables["Results"].Rows)
                //    {
                //        _interimResults.Add(dr["TraceNumber"].ToString());
                //    }

                //    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();


                //}
                //if (vm.SearchFromQuickActionFailed == true)
                //{
                //    string query = @"select c.TraceNumber as trace from Claim c where c.ClaimStatusId = 8100 and c.ReceivedDate >= '" + vm.InputProcessedDateFrom + "'" + @"
                //                    and c.ReceivedDate <='" + vm.InputProcessedDateTo + "'" + @"
                //                    and c.ClientCode in (" + _clientCodeStringForAdoQUery + @")
                //                    and c.Claimid = (select max(ic.Claimid) from Claim ic where ic.TraceNumber=c.TraceNumber) order by ReceivedDate desc";

                //    string failedClaimsTraceNumbersFromProcessedDateTime = @"SELECT InfinediTraceNumber FROM[ReadReports].[dbo].[ClaimAcceptance] ca
                //                                    WITH(INDEX(IDX_FILETYPE_CLAIM_ACCEPTED))
                //                                    join ReadReports.dbo.Claim c on ca.ClaimID = c.ClaimID
                //                                    where Accepted = 0 and FileDateTime >= '" + vm.InputProcessedDateFrom + @"'
                //                                    and c.InfinediAccountNumber in (" + _clientCodeStringForAdoQUery + @")
                //                                    and c.ClaimID = (select max(d.ClaimID) from CLaim d where d.InfinediTraceNumber = c.InfinediTraceNumber)
                //                                    and ca.ClaimAcceptanceID = (select max(ca2.claimacceptanceID) from ClaimAcceptance ca2 WITH(INDEX(IDX_FILETYPE_CLAIM_ACCEPTED))  where c.claimid = ca2.ClaimID )
                //                                    order by  claimacceptanceid desc";
                //}
                //if (vm.SearchFromQuickActionPending == true)
                //{
                //    string query = @"SELECT c.TraceNumber as trace from Claim c 
                //                    where c.ClaimId = (select max(claimid) from Claim c2 where c2.TraceNumber = c.TraceNumber )
                //                    and c.ClientCode in (" + _clientCodeStringForAdoQUery + ") " +
                //         "and c.ClaimStatusId >= 9000 and c.ClaimStatusId != 9998 and c.ClaimStatusId != 9997 " +
                //         "and c.TraceNumber is not null  and  c.ReceivedDate >= '" + vm.InputProcessedDateFrom + "' " +
                //         "and c.ReceivedDate <= '" + vm.InputProcessedDateTo + "' order by c.ReceivedDate desc ";

                //    DataSet ds = new DataSet();
                //    ds = DBFactories.DBFactory.DbCommandSelect("InfClaims", query, System.Data.CommandType.Text, null);
                //    foreach (DataRow dr in ds.Tables["Results"].Rows)
                //    {
                //        _traceNumbersList.Add(dr["TraceNumber"].ToString());
                //    }
                //}
                #endregion


                //Search by Patient Information
                if (vm.InputPatientFirstName != null)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PatientFirstName with in Daterange of claims
                    _interimResults = SearchByPatientFirstName(_clientCodeStringForAdoQUery, vm.InputPatientFirstName, vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _userTable);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();

                }
                if (vm.InputPatientLastName != null)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PatientLastName with in Daterange of claims
                    _interimResults = SearchByPatientLastName(_clientCodeStringForAdoQUery, vm.InputPatientLastName, vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _userTable);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();


                }
                if (vm.InputPatientAccountNumber != null)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PatientAccNumber with in Daterange of claims
                    _interimResults = SearchByPatientAccountNumber(_clientCodeStringForAdoQUery, vm.InputPatientAccountNumber, vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _userTable);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }

                //Search by Payer Name
                if (vm.InputPayerName != null)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PayerName with in Daterange of claims
                    _interimResults = SearchByPayerName(_clientCodeStringForAdoQUery, vm.InputPayerName, vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _userTable);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }

                //Search by CLaimtype - Inst, Prof, Dental 
                if (vm.InputIncludeProfessional == true || vm.InputIncludeInstitutional == true || vm.InputIncludeDental == true || vm.InputIncludeAll)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PayerName with in Daterange of claims
                    _interimResults = SearchByClaimType(_clientCodeStringForAdoQUery, vm.InputIncludeProfessional, vm.InputIncludeInstitutional, vm.InputIncludeDental, vm.InputIncludeAll
                        , vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _userTable);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }
                //Search by Workers Comp type
                if (vm.InputIncludeWorkerCompType == true)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PayerName with in Daterange of claims
                    _interimResults = SearchByWorkersCompType(_clientCodeStringForAdoQUery, vm.InputIncludeWorkerCompType, vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _userTable);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }
                //search by Webstatus
                if (vm.InputIncludeArchived || vm.InputIncludePending || vm.InputIncludeViewed || vm.InputIncludeOnHold || vm.InputIncludeIgnore || vm.InputIncludeAttention)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PayerName with in Daterange of claims
                    _traceNumbersList = SearchByWebStatus(_clientCodeStringForAdoQUery, vm.InputIncludeArchived, vm.InputIncludePending, vm.InputIncludeViewed,
                        vm.InputIncludeOnHold, vm.InputIncludeAttention, vm.InputIncludeIgnore,
                        vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _traceNumbersList,_userTable);
                }

                //Search from QuickActions OR (used radio buttons -Accepted , Errored, Failed , All)
                if (vm.SearchFromQuickActionAccepted || vm.SearchFromQuickActionFailed || vm.SearchFromQuickActionPending)
                {
                    List<string> _interimResults = new List<string>();
                    //GetTraceNumberFor PayerName with in Daterange of claims
                    _interimResults = SearchByProcessingStatus(_clientCodeStringForAdoQUery, vm.SearchFromQuickActionAccepted, vm.SearchFromQuickActionFailed,
                        vm.SearchFromQuickActionPending,
                        vm.InputProcessedDateFrom, vm.InputProcessedDateTo, _traceNumbersList);
                    _traceNumbersList = _traceNumbersList.Intersect(_interimResults).ToList();
                }
            }
            catch (Exception e)
            {

                string _methodForErrorLog = "GetTraceNumbersForSearch";
                LogError(_methodForErrorLog, vm, clientCodeString, e.Message, e.InnerException?.ToString() ?? "");
                _traceNumbersList = null;

            }
            return _traceNumbersList;

        }

        private static void LogError(string _methodForErrorLog, ClaimsProcessingViewModel vm, string _undecorated_CommaSeparated_ClientCodes, string errorMessage, string innerExceptionMessage)
        {
            string _connectionstringName = "WebPortal";
            CommandType _type = CommandType.Text;
            string value = "";

            value = "Tracenumber = " + vm.InputTraceNumber + "| Payername = " + vm.InputPayerName + "| ProcessedDateFrom = " + vm.InputProcessedDateFrom +
                      "| ProcessedDateTo = " + vm.InputProcessedDateTo + "| PatientLAstName = " + vm.InputPatientLastName + "| PatientFirstName = " + vm.InputPatientFirstName +
                      "| PatientCount = " + vm.InputPatientAccountNumber + "| Archived = " + vm.InputIncludeArchived + "| Pending = " + vm.InputIncludePending + "| Viewed = " + vm.InputIncludeViewed +
                      "| Attention = " + vm.InputIncludeAttention +
                      "| OnHold = " + vm.InputIncludeOnHold + "| Ignore = " + vm.InputIncludeIgnore + "| NumberofResultsPerPage = "
                      + vm.selectedPerPage + "| CurrentPageNumber = " + vm.CurrentPageNumber +
                      "| OUT-TotalNumberOfRecords = " + 0 + "| SortonColumn = " + vm.SortOnColumn + "| SortOrder = " + vm.SortType +
                      "| SearchFromQuickActionAccepted = " + vm.SearchFromQuickActionAccepted +
                      "| SearchFromQuickActionPending = " + vm.SearchFromQuickActionPending + "| SearchFromQuickActionFailed" + vm.SearchFromQuickActionFailed + "| SearchFromBatchClean = " + vm.SearchFromBatchClean +
                      "| SearchFromBatchErrored = " + vm.SearchFromBatchErrored + "| SearchFromBatchRejected = " + vm.SearchFromBatchRejected +
                      "| InputFiledId = " + vm.InputInputFileID;



            string query = "Insert into ErrorLog values('" + _undecorated_CommaSeparated_ClientCodes + "','" + value + "','" + errorMessage + "','" + innerExceptionMessage + "','" + DateTime.Now + "','" + _methodForErrorLog + "')";
            DBFactories.DBFactory.RunInsertQuery(_connectionstringName, query, _type, null);
        }

        private static List<string> SearchByProcessingStatus(string _clientCodeStringForAdoQUery, bool searchFromQuickActionAccepted, bool searchFromQuickActionFailed,
            bool searchFromQuickActionPending, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo, List<string> inputTraceNumbersList)
        {

            List<string> _resultTraceNumbers = new List<string>();
           
           

            if (searchFromQuickActionAccepted)
            {
                string _inputClaimsString="";
                //List<string> NonerroredClaims = new List<string>();
                //string connectionstringName = "WebPortal";
                //CommandType type = CommandType.Text;
                //string query = @"SELECT distinct c.TraceNumber as trace from Claim c 
                //                    where
                //                     c.ClientCode in (" + _clientCodeStringForAdoQUery +") " +
                //                  "and c.ClaimStatusId < 9000" +
                //                  "and c.TraceNumber is not null  and  c.ReceivedDate >= '" + inputProcessedDateFrom + "' " +
                //                  "and c.ReceivedDate <= '" + inputProcessedDateTo + "'";
                //NonerroredClaims.AddRange(DBFactories.DBFactory.RunQuery(connectionstringName, query, type, null));
               if (inputTraceNumbersList.Count != 0)
                {
                     _inputClaimsString = inputTraceNumbersList.Aggregate("", (current, client) => current + "'" + client + "',");
                  _inputClaimsString = _inputClaimsString.Substring(0, _inputClaimsString.Length - 1);
                }
                else
                {
                    _inputClaimsString = "''";
                }

                List<string> _claimsWithReports = new List<string>();
                string _connectionstringName = "ClaimReports";
                CommandType type = CommandType.Text;
                string queryToReports = @"SELECT distinct c.InfinediTraceNumber as Tracenumber from Claim c 
                                    where
                                     c.InfinediTraceNumber  in (" + _inputClaimsString + ") ";

                DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, queryToReports, type, null);
                _claimsWithReports = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);
                _resultTraceNumbers.AddRange(inputTraceNumbersList.Except(_claimsWithReports));

            }
            if (searchFromQuickActionFailed)
            {
                List<string> _failedReportsClaims = new List<string>();
                string _connectionstringName = "WebPortal";
                CommandType type = CommandType.StoredProcedure;
                string _SPName = "GetQuickActionNewFailedClaims";
                List<SqlParameter> _spparams = new List<SqlParameter>()
                            {
                                    new SqlParameter() {ParameterName = "ClientCode", Value = _clientCodeStringForAdoQUery.Replace("'","")},
                                    new SqlParameter() {ParameterName = "DateFrom", Value = inputProcessedDateFrom }

                            };
                
                DataSet _ds = DBFactories.DBFactory.RunQuery(_connectionstringName, _SPName, type, _spparams);
                //string query = @"SELECT InfinediTraceNumber as Tracenumber FROM[ReadReports].[dbo].[ClaimAcceptance] ca
                //                                    WITH(INDEX(IDX_FILETYPE_CLAIM_ACCEPTED))
                //                                    join ReadReports.dbo.Claim c on ca.ClaimID = c.ClaimID
                //                                    where Accepted = 0 and FileDateTime >= '" + inputProcessedDateFrom + @"'
                //                                    and c.InfinediAccountNumber in (" + _clientCodeStringForAdoQUery + @")
                //                                    and c.ClaimID = (select max(d.ClaimID) from CLaim d where d.InfinediTraceNumber = c.InfinediTraceNumber)
                //                                    and ca.ClaimAcceptanceID = (select max(ca2.claimacceptanceID) from ClaimAcceptance ca2 WITH(INDEX(IDX_FILETYPE_CLAIM_ACCEPTED))
                //                                    where c.claimid = ca2.ClaimID )
                //                                    order by  claimacceptanceid desc";


//                string query = @"select distinct c.claimid,claimacceptanceid,InfinediTraceNumber,Accepted
//                                                    into #temp 
//													FROM[ReadReports].[dbo].[ClaimAcceptance]
//        ca
//join ReadReports.dbo.Claim c on ca.ClaimID = c.ClaimID
//where FileDateTime >=  '" + inputProcessedDateFrom + @"'
//                                                    and c.InfinediAccountNumber in (" + _clientCodeStringForAdoQUery + @")

//                                                    select max(ClaimID) as MaxClaimId,InfinediTraceNumber
//                                                    into #maxclaim
//													 from #temp group by InfinediTraceNumber
												
//												select max(ClaimAcceptanceId) as MaxCAId
//                                                into #maxCA
//												from  #maxclaim maxc
//												join ClaimAcceptance ca on ca.ClaimID=maxc.MaxClaimId
//                                                group by ca.ClaimID

//                                                select distinct InfinediTraceNumber as TraceNumber
//                                                 from #temp temp
//												 join #maxCA maxca on temp.ClaimAcceptanceID=maxca.MaxCAId
//												 where accepted = 0 order by InfinediTraceNumber

//                                                 drop table #maxCA
//												 drop table #maxclaim
//												 drop table #temp";
//                DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, null);
                   
                _failedReportsClaims = ReadTraceAndReportStatusFromResultDataset(s_columnNamesToRetrieve, s_tableName, _ds );
                _resultTraceNumbers.AddRange(_failedReportsClaims.Intersect(inputTraceNumbersList));


            }
            if (searchFromQuickActionPending)
            {
                List<string> _erroredTraceNumbers = new List<string>();
                string _connectionstringName = "InfClaims";
                CommandType type = CommandType.Text;
                string query = @"SELECT c.TraceNumber as TraceNumber from Claim c 
                                    where c.ClaimId = (select max(claimid) from Claim c2 where c2.TraceNumber = c.TraceNumber )
                                    and c.ClientCode in (" + _clientCodeStringForAdoQUery + ") " +
                                   "and c.ClaimStatusId >= 9000 and c.ClaimStatusId != 9998 and c.ClaimStatusId != 9997 " +
                                   "and c.TraceNumber is not null  and  c.ReceivedDate >= '" + inputProcessedDateFrom + "' " +
                                   "and c.ReceivedDate <= '" + inputProcessedDateTo + "' order by c.ReceivedDate desc ";
                DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, null);
                _erroredTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);
                _resultTraceNumbers.AddRange(_erroredTraceNumbers);
            }
            return _resultTraceNumbers;
        }

        private static List<string> ReadTraceAndReportStatusFromResultDataset(List<string> s_columnNamesToRetrieve, string s_tableName, DataSet ds)
        {
            List<string> _ts = new List<string>();
            if (ds.Tables.Contains("Results"))
            {
                if (ds.Tables["Results"].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables["Results"].Rows)
                    {
                        TraceAndReportStatus _eachts = new TraceAndReportStatus();
                        _eachts.TraceNumber = dr["Tracenumber"].ToString();
                        _eachts.Accepted = dr["Accepted"].ToString();
                        if (_eachts.Accepted=="False")
                        _ts.Add(_eachts.TraceNumber);
                    }
                }

            }

            return _ts;
        }

        private static List<string> ReadFromResultDataset(List<string> s_columnNamesToRetrieve, string s_tableName, DataSet ds)
        {

            List<string> _resultTraceNumbers = new List<string>();
            if (ds.Tables.Contains("Results"))
            {
                if (ds.Tables["Results"].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables["Results"].Rows)
                    {

                        _resultTraceNumbers.Add(dr["Tracenumber"].ToString());

                    }
                }

            }

            return _resultTraceNumbers;
        }


        private static List<string> SearchByWebStatus(string _clientCodeStringForAdoQUery, bool inputIncludeArchived, bool inputIncludePending, bool inputIncludeViewed,
            bool inputIncludeOnHold, bool inputIncludeAttention, bool inputIncludeIgnore, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo, List<string> _traceNumbersList,DataTable InputTraceNumbers)
        {
            List<string> _resultTraceNumbers = new List<string>();
            List<string> _archivedTraceNumbers = new List<string>();
            List<string> _pendingTraceNumbers = new List<string>();
            List<string> _viewedTraceNumbers = new List<string>();
            List<string> _onHoldTraceNumbers = new List<string>();
            List<string> _attentionTraceNumbers = new List<string>();
            List<string> _ignoreTraceNumbers = new List<string>();

            if (inputIncludeArchived && inputIncludePending && inputIncludeViewed && inputIncludeOnHold && inputIncludeAttention && inputIncludeIgnore)
            {
                return _traceNumbersList;
            }
            // When Only Ignore Is Checked It Goes In Here
            else
            {
                if (_traceNumbersList.Count > 0)
                {
                    List<SqlParameter> parameters = new List<SqlParameter>();
                    SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
                    paramtr.Value = InputTraceNumbers;
                    paramtr.TypeName = "InputTraceNumbers";
                    parameters.Add(paramtr);
                    //Get status for all the listed Tracenumbers
                    string _statuses = @"SELECT s.Tracenumber,StatusDetails from claimwebstatus s 
                                         join @InputTraceNumbers ip on ip.TraceNumber = s.Tracenumber  
                                         where id = (select max(id) from claimwebstatus w2 where w2.tracenumber = s.tracenumber) ";
                    string _connectionstringName = "WebPortal";
                    List<string> _toRetrieve = new List<string>() { "Tracenumber", "StatusDetails" };
                    CommandType type = CommandType.Text;
                    DataSet _globalds = DBFactories.DBFactory.RunQuery(_connectionstringName, _statuses, type,parameters);
                    List<TraceAndStatus> _ts = new List<TraceAndStatus>();
                    _ts = ReadFromResultDataset2(_toRetrieve, s_tableName, _globalds);

                    _archivedTraceNumbers = _ts.Where(c => c.StatusDetails == "Archived").Select(c => c.TraceNumber).ToList();
                    _pendingTraceNumbers = _ts.Where(c => c.StatusDetails == "Pending").Select(c => c.TraceNumber).ToList();
                    _viewedTraceNumbers = _ts.Where(c => c.StatusDetails == "Viewed").Select(c => c.TraceNumber).ToList();
                    _onHoldTraceNumbers = _ts.Where(c => c.StatusDetails == "On Hold").Select(c => c.TraceNumber).ToList();
                    _attentionTraceNumbers = _ts.Where(c => c.StatusDetails == "Attention").Select(c => c.TraceNumber).ToList();
                    _ignoreTraceNumbers = _ts.Where(c => c.StatusDetails.Trim() == "Ignore").Select(c => c.TraceNumber.Trim()).ToList();
                    var _noStatusTraceNumbers = _traceNumbersList.Except(_ts.Select(c => c.TraceNumber)).ToList();
                    _pendingTraceNumbers.AddRange(_noStatusTraceNumbers);
                }

                if (inputIncludeArchived)
                {
                    _resultTraceNumbers.AddRange(_archivedTraceNumbers);
 
                }
                if (inputIncludePending)
                {
                    _resultTraceNumbers.AddRange(_pendingTraceNumbers);

                }
                if (inputIncludeViewed)
                {
                    _resultTraceNumbers.AddRange(_viewedTraceNumbers);
                    
                }
                if (inputIncludeOnHold)
                {
                    _resultTraceNumbers.AddRange(_onHoldTraceNumbers);
                   
                   
                }
                if (inputIncludeAttention)
                {
                    _resultTraceNumbers.AddRange(_attentionTraceNumbers);
                    
                }
                if (inputIncludeIgnore)
                {
                    _resultTraceNumbers.AddRange(_ignoreTraceNumbers);
                }
            }
            return _resultTraceNumbers;
        }

        private static List<TraceAndStatus> ReadFromResultDataset2(List<string> toRetrieve, string s_tableName, DataSet ds)
        {
            List<TraceAndStatus> _ts = new List<TraceAndStatus>();
            if (ds.Tables.Contains("Results"))
            {
                if (ds.Tables["Results"].Rows.Count > 0)
                {
                    foreach (DataRow dr in ds.Tables["Results"].Rows)
                    {
                        TraceAndStatus _eachts = new TraceAndStatus();
                        _eachts.TraceNumber = dr["Tracenumber"].ToString();
                        _eachts.StatusDetails = dr["StatusDetails"].ToString();

                        _ts.Add(_eachts);

                    }
                }

            }

            return _ts;
        }

        private static List<string> SearchByWorkersCompType(string _clientCodeStringForAdoQUery, bool inputIncludeWorkerCompType, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo,DataTable InputTraceNumbers)
        {
            List<string> _resultTraceNumbers = new List<string>();

            string _connectionstringName = "InfClaims";
            CommandType type = CommandType.Text;
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
            paramtr.Value=InputTraceNumbers;
            paramtr.TypeName = "InputTraceNumbers";
            parameters.Add(paramtr);
            string query = "SELECT [Claim].[TraceNumber] from CLAIMPROCESSING.DBO.claim " +
                                "join CLAIMPROCESSING.DBO.claimpayer on claim.claimid = claimpayer.claimid " +
                                "join InfinediPayers.dbo.Payer on Payer.payerid = claimpayer.payerid " +
                                "join InfinediPayers.dbo.TransmitDistribution on Payer.ProfessionalTransmitDistributionId = TransmitDistribution.TransmitDistributionId OR Payer.InstitutionalTransmitDistributionId = TransmitDistribution.TransmitDistributionId " +
                            
                               " join  @InputTraceNumbers ip on ip.TraceNumber=Claim.Tracenumber "+
                                "where ([Claim].[ReceivedDate] >= '" + inputProcessedDateFrom + "') " +
                                "AND ([Claim].[ReceivedDate] <= '" + inputProcessedDateTo + "') " +
                                "AND ([Claim].[ClaimStatusId] <> 9998) " +
                                "AND [Claim].[ClientCode] IN (" + _clientCodeStringForAdoQUery + ")" +
                                "AND ([Claim].[TraceNumber] IS NOT NULL) " +
                                "and ClaimDistributionNumber IN('921', '922', '963') order by [Claim].[ReceivedDate] desc";

            DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);

            return _resultTraceNumbers;
        }

        private static List<string> SearchByClaimType(string _clientCodeStringForAdoQUery, bool inputIncludeProfessional, bool inputIncludeInstitutional, bool inputIncludeDental, bool inputIncludeAll,
            DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo,DataTable InputTraceNumbers)
        {
            List<string> _resultTraceNumbers = new List<string>();
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
            paramtr.Value = InputTraceNumbers;
            paramtr.TypeName = "InputTraceNumbers";
            parameters.Add(paramtr);
            if (inputIncludeAll)
            {
                string _connectionstringName = "InfClaims";
                CommandType type = CommandType.Text;
                string query = "Select claimtemp.TraceNumber as TraceNumber from " +
                                                " (select ReceivedDate, TraceNumber from Claim" +
                                                " where (ReceivedDate > = '" + inputProcessedDateFrom + "' and ReceivedDate < = '" + inputProcessedDateTo + "')" +
                                                " and ClientCode IN (" + _clientCodeStringForAdoQUery + ") and ClaimStatusId != 9998" +
                                                " and TraceNumber is not null and(ClaimTypeId != 1 OR ClaimTypeId != 2 OR ClaimTypeId != 3)) as claimtemp" +
                                                 " join  @InputTraceNumbers ip on ip.TraceNumber=claimtemp.Tracenumber " +

                                                " order by claimtemp.ReceivedDate desc";
                DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
                _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);

                return _resultTraceNumbers;

                //List<string> listOfTraceforAllClaimTypes = new List<string>();
                //using (var con = new SqlConnection(_sqlConnectionStringClaimsProcessing))
                //{

                //    con.Open();

                //    string allclaimtypequery = "Select claimtemp.TraceNumber as TraceNumber from " +
                //                               " (select ReceivedDate, TraceNumber from Claim" +
                //                               " where (ReceivedDate > = '" + vm.InputProcessedDateFrom + "' and ReceivedDate < = '" + vm.InputProcessedDateTo + "')" +
                //                               " and ClientCode IN " + sb + " and ClaimStatusId != 9998" +
                //                               " and TraceNumber is not null and(ClaimTypeId != 1 OR ClaimTypeId != 2 OR ClaimTypeId != 3)) as claimtemp" +
                //                               " order by claimtemp.ReceivedDate desc";

                //    using (SqlCommand cmd = new SqlCommand(allclaimtypequery, con))
                //    {
                //        using (SqlDataReader reader = cmd.ExecuteReader())
                //        {
                //            while (reader.Read())
                //            {
                //                listOfTraceforAllClaimTypes.Add(reader.GetString(reader.GetOrdinal("TraceNumber")));
                //            }
                //            reader.Close();
                //        }
                //        con.Close();
                //    }

                //}
                ////listOfAllTraceNumbers = returnedlist.Intersect(latestTraceNumberClaimTypeAll).ToList();
                //listOfAllTraceNumbers = returnedlist.Intersect(listOfTraceforAllClaimTypes).ToList();
            }
            else
            {
                if (inputIncludeInstitutional)
                {
                    string _connectionstringName = "InfClaims";
                    CommandType type = CommandType.Text;
                    string query = "Select claimtemp.TraceNumber as TraceNumber from " +
                                                    " (select ReceivedDate, TraceNumber from Claim" +
                                                    " where (ReceivedDate > = '" + inputProcessedDateFrom + "' and ReceivedDate < = '" + inputProcessedDateTo + "')" +
                                                    " and ClientCode IN (" + _clientCodeStringForAdoQUery + ") and ClaimStatusId != 9998" +
                                                    " and TraceNumber is not null and ClaimTypeId = 2) as claimtemp" +
                                                      " join  @InputTraceNumbers ip on ip.TraceNumber=claimtemp.Tracenumber " +
                                                    " order by claimtemp.ReceivedDate desc";
                    DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
                    _resultTraceNumbers.AddRange( ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds));


                    //var latestTraceNumberClaimTypeInstitutional = (from c in dbcontext.Claims.AsNoTracking()
                    //                                               where c.ReceivedDate >= vm.InputProcessedDateFrom
                    //                                                     && userseesclients.Contains(c.ClientCode)
                    //                                                     && c.ReceivedDate <= vm.InputProcessedDateTo
                    //                                                     && c.ClaimStatusId != 9998
                    //                                                     && c.TraceNumber != null
                    //                                                     && c.ClaimTypeId == 2
                    //                                               select c).Distinct().OrderByDescending(c => c.ReceivedDate).Select(c => c.TraceNumber).ToList();

                    //listOfInsitutionalTraceNumbers = returnedlist.Intersect(latestTraceNumberClaimTypeInstitutional).ToList();
                }
                if (inputIncludeProfessional)
                {
                    string _connectionstringName = "InfClaims";
                    CommandType type = CommandType.Text;
                    string query = "Select claimtemp.TraceNumber as TraceNumber from " +
                                                    " (select ReceivedDate, TraceNumber from Claim" +
                                                    " where (ReceivedDate > = '" + inputProcessedDateFrom + "' and ReceivedDate < = '" + inputProcessedDateTo + "')" +
                                                    " and ClientCode IN (" + _clientCodeStringForAdoQUery + ") and ClaimStatusId != 9998" +
                                                    " and TraceNumber is not null and ClaimTypeId = 1) as claimtemp" +
                                                      " join  @InputTraceNumbers ip on ip.TraceNumber=claimtemp.Tracenumber " +
                                                    " order by claimtemp.ReceivedDate desc";
                    DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
                    _resultTraceNumbers.AddRange(ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds));

                    //var latestTraceNumberClaimTypeProfessional = (from c in dbcontext.Claims.AsNoTracking()
                    //                                              where c.ReceivedDate >= vm.InputProcessedDateFrom
                    //                                                    && userseesclients.Contains(c.ClientCode)
                    //                                                    && c.ReceivedDate <= vm.InputProcessedDateTo
                    //                                                    && c.ClaimStatusId != 9998
                    //                                                    && c.TraceNumber != null
                    //                                                    && c.ClaimTypeId == 1
                    //                                              select c).Distinct().OrderByDescending(c => c.ReceivedDate).Select(c => c.TraceNumber).ToList();


                    //listOfProfessionalTraceNumbers = returnedlist.Intersect(latestTraceNumberClaimTypeProfessional).ToList();
                }
                if (inputIncludeDental)
                {

                    string _connectionstringName = "InfClaims";
                    CommandType type = CommandType.Text;
                    string query = "Select claimtemp.TraceNumber as TraceNumber from " +
                                                    " (select ReceivedDate, TraceNumber from Claim" +
                                                    " where (ReceivedDate > = '" + inputProcessedDateFrom + "' and ReceivedDate < = '" + inputProcessedDateTo + "')" +
                                                    " and ClientCode IN (" + _clientCodeStringForAdoQUery + ") and ClaimStatusId != 9998" +
                                                    " and TraceNumber is not null and ClaimTypeId = 3) as claimtemp" +
                                                      " join  @InputTraceNumbers ip on ip.TraceNumber=claimtemp.Tracenumber " +
                                                    " order by claimtemp.ReceivedDate desc";
                    DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
                    _resultTraceNumbers.AddRange(ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds));
                    //var latestTraceNumberClaimTypeDental = (from c in dbcontext.Claims.AsNoTracking()
                    //                                        where c.ReceivedDate >= vm.InputProcessedDateFrom
                    //                                              && userseesclients.Contains(c.ClientCode)
                    //                                              && c.ReceivedDate <= vm.InputProcessedDateTo
                    //                                              && c.ClaimStatusId != 9998
                    //                                              && c.TraceNumber != null
                    //                                              && c.ClaimTypeId == 3
                    //                                        select c).Distinct().OrderByDescending(c => c.ReceivedDate).Select(c => c.TraceNumber).ToList();


                    //listOfDentalTraceNumbers = returnedlist.Intersect(latestTraceNumberClaimTypeDental).ToList();
                }
            }


            return _resultTraceNumbers;
        }

        private static List<string> SearchByPayerName(string _clientCodeStringForAdoQUery, string inputPayerName, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo,DataTable InputTraceNumbers)
        {
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
            paramtr.Value = InputTraceNumbers;
            paramtr.TypeName = "InputTraceNumbers";
            parameters.Add(paramtr);
            List<string> _resultTraceNumbers = new List<string>();
            string query = "SELECT distinct [Claim].[TraceNumber],[Claim].ReceivedDate " +
                                   "FROM [Claim] AS [Claim] " +
                                    " join  @InputTraceNumbers ip on ip.TraceNumber=Claim.Tracenumber " +
                                   "INNER JOIN [ClaimPayer] AS [ClaimPayers] ON ([Claim].[ClaimId]) = [ClaimPayers].[ClaimId]" +
                                       "AND ([Claim].[ReceivedDate] >= '" + inputProcessedDateFrom + "')" +
                                       "AND ([Claim].[ReceivedDate] <= '" + inputProcessedDateTo + "')" +
                                       "AND ([Claim].[ClaimStatusId] <> 9998)" +
                                       "AND [Claim].[ClientCode] IN (" + _clientCodeStringForAdoQUery + ") " +
                                       "AND ([Claim].[TraceNumber] IS NOT NULL)" +
                                   "WHERE ([ClaimPayers].[PayerName] LIKE '%" + inputPayerName + "%')" +
                                       "AND([Claim].[TraceNumber] IS NOT NULL) order by claim.ReceivedDate Desc";
            string _connectionstringName = "InfClaims";
            CommandType type = CommandType.Text;
            DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);

            return _resultTraceNumbers;
            //var listoftracenumbersSearchedByPayer = (from x in dbcontext.Claims.AsNoTracking()
            //                                         join pa in dbcontext.ClaimPayers.AsNoTracking() on x.ClaimId equals pa.ClaimId
            //                                         where pa.PayerName.Contains(vm.InputPayerName)
            //                                               && userseesclients.Contains(x.ClientCode)
            //                                               && x.ReceivedDate >= vm.InputProcessedDateFrom
            //                                               && x.ReceivedDate <= vm.InputProcessedDateTo
            //                                               && x.ClaimStatusId != 9998
            //                                               && x.TraceNumber != null
            //                                         orderby x.ReceivedDate descending
            //                                         select x.TraceNumber).Distinct().ToList();


            //  _interimResults.Add(vm.InputTraceNumber);

        }

        private static List<string> SearchByPatientAccountNumber(string _clientCodeStringForAdoQUery, string inputPatientAccountNumber, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo
            ,DataTable InputTraceNumbers)
        {
            List<string> _resultTraceNumbers = new List<string>();
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
            paramtr.Value = InputTraceNumbers;
            paramtr.TypeName = "InputTraceNumbers";
            parameters.Add(paramtr);
            string query = "SELECT [Claim].[TraceNumber]" +
                                    "FROM [Claim] AS [Claim] INNER JOIN [ClaimIdentifier] AS [ClaimIdentifier] ON ([Claim].[ClaimId]) = [ClaimIdentifier].[ClaimId]" +
                                     " join  @InputTraceNumbers ip on ip.TraceNumber=Claim.Tracenumber " +
                                        "AND ([Claim].[ReceivedDate] >= '" + inputProcessedDateFrom + "')" +
                                        "AND ([Claim].[ReceivedDate] <= '" + inputProcessedDateTo + "')" +
                                        "AND ([Claim].[ClaimStatusId] <> 9998)" +
                                        "AND [Claim].[ClientCode] IN (" + _clientCodeStringForAdoQUery + ") " +
                                        "AND ([Claim].[TraceNumber] IS NOT NULL)" +
                                    "WHERE ([ClaimIdentifier].[IdentifierTypeId] = 68) " +
                                        "AND([ClaimIdentifier].[IdentifierValue] LIKE '%" + inputPatientAccountNumber + "%')" +
                                        "AND([Claim].[TraceNumber] IS NOT NULL) order by claim.ReceivedDate desc";
            string _connectionstringName = "InfClaims";
            CommandType type = CommandType.Text;
            DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);

            return _resultTraceNumbers;
        }

        private static List<string> SearchByPatientLastName(string _clientCodeStringForAdoQUery, string inputPatientLastName, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo,DataTable InputTraceNumbers)
        {
            List<string> _resultTraceNumbers = new List<string>();
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
            paramtr.Value = InputTraceNumbers;
            paramtr.TypeName = "InputTraceNumbers";
            parameters.Add(paramtr);
            string query = "";
            if (inputPatientLastName.Length <= 3)
            {
                query = "select vw.tracenumber from vw_PatientLastName vw"+
                     " join  @InputTraceNumbers ip on ip.TraceNumber=vw.Tracenumber " +
                     "where ClientCode in ("
                   + _clientCodeStringForAdoQUery + ") and LastName like '" + inputPatientLastName + @"%'
                            and ReceivedDate >= '" + inputProcessedDateFrom + "' and ReceivedDate <= '" + inputProcessedDateTo + @"'
                                         and vw.TraceNumber is not null and ClaimStatusId !=9998 order by ReceivedDate desc";
            }
            else
            {
                query = "select vw.tracenumber from vw_PatientLastName vw "+
                     " join  @InputTraceNumbers ip on ip.TraceNumber=vw.Tracenumber " +
                    " where ClientCode in (" + _clientCodeStringForAdoQUery + @"
                                    ) and LastName like '%" + inputPatientLastName + "%'  and ReceivedDate >= '" + inputProcessedDateFrom + @"' 
                                    and ReceivedDate <= '" + inputProcessedDateTo + "' and vw.TraceNumber is not null and ClaimStatusId !=9998 order by ReceivedDate desc";
            }
            string _connectionstringName = "InfClaims";
            CommandType type = CommandType.Text;
            DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);

            return _resultTraceNumbers;
        }

        private static List<string> SearchByPatientFirstName(string _clientCodeStringForAdoQUery, string inputPatientFirstName, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo,DataTable InputTraceNumbers)
        {
            List<string> _resultTraceNumbers = new List<string>();
            List<SqlParameter> parameters = new List<SqlParameter>();
            SqlParameter paramtr = new SqlParameter("@InputTraceNumbers", SqlDbType.Structured);
            paramtr.Value = InputTraceNumbers;
            paramtr.TypeName = "InputTraceNumbers";
            parameters.Add(paramtr);
            string query = "";
            if (inputPatientFirstName.Length <= 3)
            {
                query = @"select vw.tracenumber from vw_PatientFirstName vw "+
                     " join  @InputTraceNumbers ip on ip.TraceNumber=vw.Tracenumber " +

                                "where ClientCode in (" + _clientCodeStringForAdoQUery + @") 
                                and FirstName like '" + inputPatientFirstName + "%' and ReceivedDate >= '" + inputProcessedDateFrom + @"'
                                and ReceivedDate <= '" + inputProcessedDateTo + "' and vw.TraceNumber is not null and ClaimStatusId !=9998  order by ReceivedDate desc";
            }
            else
            {
                query = "select vw.tracenumber from vw_PatientFirstName vw "+
                     " join  @InputTraceNumbers ip on ip.TraceNumber=vw.Tracenumber " + 
                     " where ClientCode in (" + _clientCodeStringForAdoQUery + @")
                                and FirstName like '%" + inputPatientFirstName + "%' and ReceivedDate >= '" + inputProcessedDateFrom + "' and ReceivedDate <= '"
                + inputProcessedDateTo + "'  and vw.TraceNumber is not null and ClaimStatusId !=9998 order by ReceivedDate desc";
            }
            string _connectionstringName = "InfClaims";
            CommandType type = CommandType.Text;
           
            DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, parameters);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);

            return _resultTraceNumbers;
        }

        private static List<string> SearchByInputFileId(ClaimsProcessingViewModel vm, string clientCodeString)
        {
          //  if(clientCodeString.Contains(','))
            clientCodeString = clientCodeString.Substring(1, clientCodeString.Length-2);
            List<string> _resultTraceNumbers = new List<string>();
            List<SqlParameter> _spparams = new List<SqlParameter>()
                            {
                                    new SqlParameter() {ParameterName = "StartDate", Value= vm.InputProcessedDateFrom},
                                    new SqlParameter() {ParameterName = "EndDate", Value=vm.InputProcessedDateTo},
                                    new SqlParameter() {ParameterName = "ClientCodeString", Value = clientCodeString},
                                    new SqlParameter() {ParameterName = "InputFileIncludeArchived", Value = 1 },
                                    new SqlParameter() {ParameterName = "InputFileIncludePending", Value = 1},
                                    new SqlParameter() {ParameterName = "InputFileIncludeViewed", Value = 1},
                                    new SqlParameter() {ParameterName = "RowsPerPage", Value =vm.selectedPerPage},
                                    new SqlParameter() {ParameterName = "StartIndex", Value = vm.StartIndex},
                                    new SqlParameter() {ParameterName = "SortOnColumn", Value = "Date"},
                                    new SqlParameter() {ParameterName = "SortOrder", Value = "Desc"},
                                    new SqlParameter() {ParameterName = "InputFileId", Value = vm.InputInputFileID},

                            };
            string _connectionstringName = "WebPortal";
            CommandType type = CommandType.StoredProcedure;
            string _SPName = "GetListOfInputFiles";
            DataSet _ds = DBFactories.DBFactory.RunQuery(_connectionstringName, _SPName, type, _spparams);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, _ds);
            return _resultTraceNumbers;
        }

        private static List<string> SearchByProcessedDates(string _clientCodeStringForAdoQUery, DateTime? inputProcessedDateFrom, DateTime? inputProcessedDateTo)
        {
            List<string> _resultTraceNumbers = new List<string>();
            string query = @"SELECT distinct c.TraceNumber,c.ReceivedDate as Tracenumber,TransmitDate as ClaimAuditDate from Claim c 
                                    where
                                     c.ClientCode in (" + _clientCodeStringForAdoQUery + ") " +
                             "and c.ClaimStatusId != 9998" +
                             "and c.TraceNumber is not null  and  c.ReceivedDate >= '" + inputProcessedDateFrom + "' " +
                             "and c.ReceivedDate <= '" + inputProcessedDateTo + "' order by c.ReceivedDate desc";
            string _connectionstringName = "InfClaims";
            CommandType type = CommandType.Text;
            DataSet ds = DBFactories.DBFactory.RunQuery(_connectionstringName, query, type, null);
            _resultTraceNumbers = ReadFromResultDataset(s_columnNamesToRetrieve, s_tableName, ds);
            return _resultTraceNumbers;
        }


    }
    public class TraceAndStatus
    {
        public string TraceNumber { get; set; }
        public string StatusDetails { get; set; }
    }
    public class TraceAndReportStatus
    {
        public string TraceNumber { get; set; }
        public string Accepted { get; set; }
    }
}
