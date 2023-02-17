
//This code composes a body on an email when certain user action is performed. The body of email is not written in the fuction because it exists as a template.
// the code queries the database, retrieves the tempale text and copies it into a new message 
//and then cals another func that will continue processing client request
//its args are itemID (for which the action was triggered), who it was triggered by,
// the triggering change, and an argument to not run the func if so needed

   static public void SendStatusChangeEmailNotice(long itemID, string who, string newStatus, string noEmail = "false")
        {
            if (noEmail == "true")
            {
                return;
            }

//establishing connection with Azure DB employing Entity Framework and getting the info for the item object
         

            ClientContext emDB = new ClientContext(who, "StatusChangeEMailMessage", "Model");
            var l = (from ln in emDB.Items              // LINQ Query
                   where ln.ItemID == itemID
                   select ln).FirstOrDefault();

//return if item is not found
            if (l == null)
            {
         
                return;
            }

//run sql query to figure out which template needs to be used for this email 

            var query = "SendStatusChangeEmailNotice1_SP "           // SQL stored proc with parameters
                + "  @itemID=" + l.ItemID.ToString()
                + " ,@newStatus='" + newStatus + "' "
                ;
            var dt = BLQuery.RunQuery(query);                        // this returns a System.DataTable

//if  some dataset was returned, loop through it to compose one message per returned row

            if (dt != null)
            {
                for (var ri = 0; ri < dt.Rows.Count; ++ri)               // each ri is a System.DataRow
                {  
//for each row get the templateID (tID) from the returned dataser
                    var row = dt.Rows[ri];
                    var tIDstr = row["tID"]?.ToString();                 

//if tID is not null, then query the DB again to grab the template obejct with that ID 
                   
                    if (!string.IsNullOrWhiteSpace(tIDstr))

                    {
                        ClientContext db = emDB;

                        int tID = -1;
                        bool success = Int32.TryParse(tIDstr, out tID);           // out parameters pass the int by reference
                        var eTemplate = (from et in db.MessageTemplates
                                         where et.MessageTemplateID == tID
                                         select et).FirstOrDefault();
//return if not such object was found
                        if (eTemplate == null)
                        {
#if DEBUG
                            BLUtil.DebugBreak("template not defined");
#endif
                            return;
                        }
//if found, create  a new Message object and populate it with the template's data  
                        else
                        {
                                var em = new Message();

                                em.TemplateName = eTemplate.TemplateName;
                                em.Category = eTemplate.Category;

                                em.EmailTo = eTemplate.DefaultEmailTo;
                                em.EmailCC = eTemplate.DefaultEmailCC;
                                em.EmailBCC = eTemplate.DefaultEmailBCC;
                                em.EmailGroup = eTemplate.DefaultEmailGroup;

                                em.SubjectLine = eTemplate.SubjectLine;
                                em.BodyContent = eTemplate.BodyContent;
                                                         
//make a call to the save func 

                                emDB.SaveChanges();
                               
                           
                        }
                    }
                }
            }

        }


