# Client Journey Coverage

Source: `Clients Journey.docx`

| Requirement | Status | Implementation / next action |
|---|---|---|
| Login | Implemented | Admin-created accounts, Admin/Client roles, signed sessions |
| Three product choices | MVP implemented | Cohort, 1:1 coaching, and Career Survival Library catalog |
| Payment | Foundation ready | Orders/payment references schema added; payment provider credentials still required |
| 1:1 focus dropdown and booking calendar | MVP implemented | Focus and date/time selection; persistent operational booking schema added |
| Email meeting request | Test-mode workflow | Email and calendar outbox records are created after booking; provider worker required |
| Career Survival video library | MVP implemented | Library modules exist; Admin-managed product/lesson UI and real video content remain |
| Bundle and individual course pricing | Foundation ready | Parent product, bundle, course, and independent price schema added |
| Cohort welcome email | Test-mode workflow | Welcome email outbox record is created after enrollment |
| WhatsApp/Discord group | Foundation ready | Cohort community type/link and notification channels added; provider/link configuration remains |
| Kickoff session | Foundation ready | Cohorts and scheduled session schema added |
| Client progress dashboard | Implemented | Enrollment, lesson progress, bookings, and career identity |
| Week 1 strengths assessment | MVP implemented | In-platform assessment and progress |
| Upload Character Strengths results | Foundation ready | Client upload and asset relations added; client upload UI/API remains |
| Recorded strengths videos | Foundation ready | Video lesson/content URL model added; real videos and Admin lesson management remain |
| End-of-week group meeting | Foundation ready | Cohort sessions and calendar outbox available; scheduling UI remains |
| Week 2 interests, values, purpose forms | MVP implemented | In-platform forms and stored results |
| Auto-generated career identity | Implemented | Combines four assessment summaries and suggested paths |
| Downloadable career identity | Pending | PDF/document generation endpoint and template required |
| Week 3 30-minute 1:1 | MVP implemented | Booking exists; availability rules and real calendar provider remain |
| Week 4 career paths and CV template | Partial | Suggested paths exist; downloadable CV templates and upload flow remain |
| LinkedIn/job-search live session | Foundation ready | Scheduled cohort session model added |
| Interview live session | Foundation ready | Scheduled cohort session model added |
| Week 5 library bonus | MVP implemented | Cohort catalog includes library bonus lesson; entitlement automation remains |
| Landing cohort video | CMS implemented | Admin can upload video and add a homepage video section |
| About section with pictures | CMS implemented | Editable content and image upload |
| Testimonials | CMS implemented | Editable bilingual testimonial collection |

## External configuration required

- Payment provider and webhook credentials.
- Transactional email provider and sender identity.
- Google or Microsoft Calendar OAuth/application credentials.
- WhatsApp Business API or Discord integration details, if automatic group invitations are required.
- Final video files, CV templates, assessment rules, and downloadable career-identity design.
