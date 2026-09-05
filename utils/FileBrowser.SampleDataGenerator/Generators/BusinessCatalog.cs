internal sealed record BusinessDepartment(string Name, BusinessTeam[] Teams);

internal sealed record BusinessTeam(
    string Name, string Initiative, string Deliverable, string Purpose,
    string Evidence, string Risk, string Decision, string Action);

internal static class BusinessCatalog
{
    public static readonly BusinessDepartment[] Departments =
    [
        new("Finance", [
            new("Financial Planning and Analysis", "Operating Budget Review", "Budget Variance Analysis",
                "Explain operating expense variance and prepare the next quarterly forecast.",
                "The approved quarterly operating budget is USD 1,200,000. Actual expense is USD 1,284,000, an unfavorable variance of USD 84,000 (7%). Contractor costs account for USD 60,000 and software renewals for USD 24,000 of the variance.",
                "Unapproved contractor extensions could increase the forecast beyond the approved spending envelope.",
                "Require Finance approval for contractor extensions and defer USD 24,000 of optional software purchases.",
                "Reconcile contractor purchase orders with Procurement and submit a revised forecast with separate labor and software assumptions."),
            new("Accounts Payable", "Supplier Invoice Reconciliation", "Invoice Exception Report",
                "Resolve supplier payment exceptions using purchase order, receipt, and invoice matching.",
                "The exception queue contains 18 invoices totaling USD 96,400. Twelve invoices lack goods receipts; six contain unit prices above the purchase order. No exception invoice has been released for payment.",
                "Paying unmatched invoices may cause duplicate payments or charges for undelivered goods.",
                "Keep disputed invoices on payment hold until a three-way match or documented exception approval is available.",
                "Ask receiving teams for the twelve missing receipts and request corrected invoices for the six price discrepancies.")]),
        new("Marketing", [
            new("Demand Generation", "Enterprise Webinar Campaign", "Campaign Performance Report",
                "Measure webinar lead generation and improve conversion to qualified sales opportunities.",
                "The campaign spent USD 18,000 and generated 600 registrations, 240 attendees, and 48 marketing-qualified leads. Cost per qualified lead is USD 375; attendance rate is 40%.",
                "Incomplete campaign attribution may overstate paid-channel performance and obscure organic conversions.",
                "Keep the next campaign within USD 18,000 and require source attribution before increasing paid media spending.",
                "Audit tracking parameters, publish the webinar recording, and have Sales follow up with all 48 qualified leads."),
            new("Brand and Content", "Customer Story Launch", "Editorial Brief",
                "Publish an approved customer success story for the enterprise product launch.",
                "The draft describes a fictional customer, Cedar Logistics, reducing monthly reporting time from 40 hours to 24 hours, a 40% reduction. Customer approval and brand review remain pending.",
                "Publishing an unapproved quotation or unsupported savings claim could damage customer trust.",
                "Publish only after written customer consent and Legal approval of performance claims.",
                "Validate the reporting-time calculation, collect quotation approval, and prepare web and sales-enablement versions.")]),
        new("Legal", [
            new("Commercial Contracts", "Cedar Logistics Renewal", "Contract Review Memo",
                "Review the master services agreement renewal and document negotiation positions.",
                "The proposed renewal term is twelve months with a USD 240,000 annual service fee. Open clauses concern a liability cap, service credits, and a 60-day non-renewal notice. These are fictional negotiation terms, not legal guidance.",
                "An uncapped general liability clause would create exposure disproportionate to the annual contract value.",
                "Propose a general liability cap equal to twelve months of fees, with exceptions subject to counsel review.",
                "Send the liability redline to counsel and confirm the notice deadline with the account manager before signature."),
            new("Privacy and Compliance", "Vendor Data Processing Review", "Privacy Assessment",
                "Assess a proposed analytics vendor's handling of customer contact information.",
                "The proposed data flow includes business email addresses and product usage events. The vendor requests 180-day retention; the internal requirement is 90 days. Subprocessor locations are not yet confirmed.",
                "Unverified subprocessors and excessive retention could violate agreed customer data handling requirements.",
                "Do not authorize production data transfer until the retention period and subprocessor inventory are approved.",
                "Obtain the data processing addendum, validate deletion controls, and document the approved subprocessor locations.")]),
        new("Human Resources", [
            new("Talent Acquisition", "Customer Success Hiring", "Recruiting Pipeline Review",
                "Fill three approved customer success positions within the staffing budget.",
                "The pipeline contains 36 applicants, twelve completed screenings, six panel interviews, and two pending offers. Three positions are approved; one role has no finalist.",
                "Delayed interviews could leave onboarding coverage below the planned staffing level.",
                "Prioritize the remaining panel interviews and keep offers within the approved compensation bands.",
                "Reserve interview slots with hiring managers and prepare onboarding plans for accepted offers."),
            new("Learning and Development", "Manager Onboarding Program", "Training Completion Report",
                "Prepare new managers to conduct feedback sessions and consistent performance reviews.",
                "Twenty managers are enrolled. Sixteen completed the feedback workshop and twelve completed the performance calibration module. Eight managers still need the calibration module.",
                "Incomplete calibration training may lead to inconsistent performance ratings across teams.",
                "Require completion of both modules before managers submit their first performance reviews.",
                "Schedule a calibration workshop for the eight remaining participants and collect completion evidence.")]),
        new("Operations", [
            new("Procurement", "Software License Optimization", "Renewal Savings Analysis",
                "Reduce unused software seats while preserving access for active staff.",
                "The license inventory lists 500 seats at USD 20 per seat per month. Only 380 seats are assigned to active users. Removing 120 unused seats would save USD 2,400 monthly or USD 28,800 annually.",
                "Reducing seats before team validation could interrupt access for seasonal workers.",
                "Renew 380 seats after department owners confirm demand and the vendor confirms pricing.",
                "Validate the inactive-seat list with IT and obtain a revised vendor quote before the renewal deadline."),
            new("Facilities", "Office Access Modernization", "Rollout Readiness Review",
                "Replace office badge readers without interrupting employee building access.",
                "Eight of ten badge readers passed acceptance testing. The two loading dock readers failed offline access tests. Reception readers and emergency exit hardware passed inspection.",
                "A network outage could prevent authorized loading dock access if offline credentials are unavailable.",
                "Delay loading dock deployment until both readers pass offline access tests.",
                "Have the installer update reader firmware, repeat outage testing, and record Facilities sign-off.")]),
        new("Sales", [
            new("Enterprise Accounts", "Regional Pipeline Review", "Sales Forecast",
                "Review enterprise opportunities and distinguish committed revenue from early-stage pipeline.",
                "The regional pipeline totals USD 900,000 across nine opportunities. Three opportunities totaling USD 300,000 have confirmed procurement schedules; the other six totaling USD 600,000 remain in discovery or evaluation.",
                "Including evaluation-stage deals in the committed forecast could overstate expected bookings.",
                "Report USD 300,000 as the procurement-confirmed segment, with signature risk disclosed separately.",
                "Confirm approval milestones for the three advanced opportunities and update the CRM next steps for the remaining six."),
            new("Customer Success", "Renewal Health Program", "Account Health Review",
                "Identify renewal risk and coordinate recovery plans for enterprise customers.",
                "Twenty accounts renew next quarter. Fifteen are healthy, three need adoption support, and two have unresolved priority support cases. The five at-risk accounts represent USD 180,000 in annual recurring revenue.",
                "Low product adoption and unresolved support cases may reduce renewal probability.",
                "Assign a recovery plan to each of the five at-risk accounts before renewal proposals are sent.",
                "Schedule adoption workshops for three accounts and escalate the two priority cases to Support with named owners.")])
    ];
}
