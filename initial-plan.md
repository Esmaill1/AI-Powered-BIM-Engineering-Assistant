# Project plan: AI-assisted Revit model QA/QC checker

**One-line pitch:** a Revit add-in where a BIM coordinator describes a model rule in plain language, such as "every door needs a fire rating", and the tool turns it into a check, runs it, and gives a clickable issue report. It reports problems and never edits the design.

> [!NOTE]
> **Superceded Specification:** This preliminary draft plan has been formally revised and ratified following the grill-me design alignment. For the authoritative technical design, headless snapshot architecture, updated thesis-first scope, and formal evaluation plan, refer directly to [PRD_Revit_QAQC_Checker.md](file:///c:/Users/esmail/Desktop/revit%20ai/PRD_Revit_QAQC_Checker.md) (v2.0) and the ratified [design_decisions.md](file:///c:/Users/esmail/.gemini/antigravity-ide/brain/055f7359-ad09-405f-a37b-f26a656b7fdb/design_decisions.md).

This plan is a proposal for your team, not a validated product spec. Several parts, especially the market and pricing, still need to be confirmed through your interviews.

## 1. Problem and users

**The problem:** BIM teams must check that models follow project rules before sharing them. ISO 19650 describes quality checks and review as formal steps, and a failed check must be recorded with the amendments needed. The BIM Execution Plan sets naming conventions, file structure, and review processes that models must follow. Checking these by hand is slow, and rules often live in documents rather than in tools. [atbim](https://www.atbim.com/en/blog/iso-19650)

**Why it can be automated:** The UK BIM Framework says that where information is specified in a structured way, acceptance criteria can be turned into automated rules. Your tool's job is to make that conversion easy. [ukbimframework](https://www.ukbimframework.org/wp-content/uploads/2021/02/Guidance-Part-D_Developing-information-requirements_Edition-2.pdf)

**Target users:**
- **Primary:** BIM coordinators and managers at small and mid-sized architecture and engineering firms.
- **Secondary:** BIM modelers who self-check before sharing a model.

## 2. Competitor landscape

| Tool | What it offers | Gap for you |
|---|---|---|
| **Solibri** | Rule-based model checking with rulesets and templates; it is described as a leader in BIM QA. [solibri](https://www.solibri.com/solutions/bim-quality-assurance/data-validation) | Premium tool that works on IFC, not inside Revit. An older review priced it from £5,400, so check current pricing. [aecmag](https://aecmag.com/news/solibri-model-checker-v8/) |
| **Autodesk Model Checker for Revit** | Free tool that checks Revit models against BIM requirements and produces a compliance report. [interoperability.autodesk](https://interoperability.autodesk.com/modelchecker.php) | No plain-language rule creation (verify this when you test it). |
| **DIY Revit scripts and add-ins** | Practitioners share custom model-health scripts and dashboards. [linkedin](https://www.linkedin.com/posts/slantis_firm-a-has-no-time-for-qaqc-firm-b-has-activity-7429933262057345024-QJl-) | Not packaged, supported, or easy for non-programmers. |

**Your wedge:** the person writing the rules should not need programming. Solibri's own rules are built from templates, and a Solibri datasheet describes them as templates, rulesets, and roles. Test whether writing a rule in plain language is a real improvement over templates. [solibri-assets.s3.amazonaws](https://solibri-assets.s3.amazonaws.com/Datasheets/Solibri_SMC_Roles_and_Rulesets_Marketing_Datasheet.pdf)

## 3. Product scope

### MVP (must have)

1. **Revit add-in** with a simple panel, using the Revit API to read elements and parameters. The API's `FilteredElementCollector` is the standard way to search and filter elements in a document or view. [revitapidocs](https://www.revitapidocs.com/2026/263cf06b-98be-6f91-c4da-fb47d01688f3.htm)
2. **Rule engine** for a fixed set of check types:
   - Required parameter exists and is not empty.
   - Naming convention matches a pattern.
   - Value is within an allowed list or range.
   - Elements are placed, for example rooms or level assignment.
   - Duplicate or unused items.
3. **Plain-language rule authoring:** a language model converts a sentence into a structured rule (JSON). The user sees and approves it before it runs.
4. **Issue report:** a list with severity, element ID, and a button to select and zoom to the element in Revit.
5. **Export:** CSV or HTML report, plus a saved rule set that can be reused across projects.

### Stretch (only if time allows)

- Import rules from a BIM Execution Plan table (Excel).
- A dashboard showing model health over time.
- Rule packs for one standard, such as a naming convention.
- IFC checking without Revit.

### Out of scope

Changing model geometry, auto-fixing, clash resolution, and code compliance certification.

## 4. Architecture

The key design choice is that the AI only creates rules, and a fixed engine does the checking.

```
User sentence → LLM → structured rule (JSON) → user approves
                                        ↓
Revit add-in → read model data → rule engine → issues → report UI
```

- **Rule schema:** a defined JSON format with a small set of check types. The LLM output must validate against it, and invalid output is rejected.
- **Execution:** the check engine is plain C# code, so results are repeatable. The same rule on the same model gives the same answer.
- **Safety:** the add-in only reads the model, so it can't damage a project.
- **Privacy:** send the LLM only the user's sentence and parameter names, not model contents. This is easier to explain to firms than sending whole models.

## 5. Evaluation plan

This is where the project earns its grade.

1. **Test models:** download open Revit and IFC models. BIM4LCA offers architectural, structural, and HVAC models, and BIMcollab offers a native Revit example project. [nordicsustainableconstruction](https://www.nordicsustainableconstruction.com/knowledge/2024/august/bim4lca-files)
2. **Error injection:** create copies of a model with known errors (remove parameters, rename elements, add duplicates). Count how many your tool catches.
3. **Metrics:**
   - Precision and recall of detected issues.
   - Check run time on models of different sizes.
   - Rule translation accuracy: of 50 or more sentences, how many become the correct rule.
   - Time to write a rule, compared with Autodesk Model Checker and a template-based approach.
4. **User test:** 5 to 10 BIM people each write the same 5 rules in your tool and in a baseline, and you record time and errors.

## 6. Business plan (your product and business angle)

**Customer discovery:** interview 8 to 10 BIM coordinators. Ask how they check models today, how long it takes, which rules cause the most trouble, and what they use now. Also ask what they pay for, who approves software purchases, and whether they'd send any model data to an AI service.

**Pricing:** don't fix a price before interviews. Compare with Solibri and the free Autodesk checker, then test two or three price points with interviewees. [solibri](https://www.solibri.com/solutions/bim-quality-assurance/data-validation)

**Business section contents:**
- Customer segments and the problem each has.
- Competitor comparison and your wedge.
- Cost to build and maintain, including Revit version updates.
- Pricing options and a break-even estimate that you label as assumptions.
- Risks and a go-to-market idea, such as free for students and small firms or a paid team plan.

## 7. Timeline (23 Oct to early Feb)

| Dates | Milestone | Output |
|---|---|---|
| 23 Oct to 6 Nov | Discovery and setup | Interview outreach, competitor review, test models, Revit add-in "hello world", rule schema draft |
| 7 to 27 Nov | Core build | Element reader, 5 rule types, basic issue list |
| 28 Nov to 18 Dec | AI and UX | Plain-language rule translation, approval step, zoom to element, export |
| 19 Dec to 10 Jan | Evaluation | Error-injection tests, translation accuracy test, bug fixes |
| 11 to 25 Jan | User pilot and business | User test sessions, pricing feedback, business section |
| 26 Jan to early Feb | Finish | Thesis, demo video, limitations, final presentation |

Freeze new features around 10 January so you have time to evaluate and write.

## 8. Team roles

| Role | Responsibility |
|---|---|
| **Revit developer** | C# add-in, API access, selection and zoom, installer |
| **Rules and AI engineer** | Rule schema, LLM translation, validation, prompt tests |
| **UI and reporting developer** | Panel, report, export, rule library |
| **Research and business lead** | Interviews, competitors, test datasets, error-injection design, pricing, thesis structure |

Everyone should write tests, and the roles should rotate for reviews.

## 9. Risks and mitigation

| Risk | Mitigation |
|---|---|
| Revit API learning curve slows the team | Build a minimal add-in in week one and share it as a template |
| Free Autodesk Model Checker seems "good enough" [interoperability.autodesk](https://interoperability.autodesk.com/modelchecker.php) | Compare directly on rule-writing time and flexibility, and report honestly |
| LLM misreads a rule | Always show a structured preview and require approval, and measure accuracy |
| No access to BIM professionals | Use public BIM guidelines and standards documents to build rule sets, and ask university staff or alumni |
| Revit version changes | Target one Revit version and state it |
| Scope creep | Freeze scope on 10 Jan and move extras to future work |

## 10. Deliverables

- Working Revit add-in, with an installer or setup instructions.
- Source code repository and documentation.
- Evaluation report with metrics and test data.
- Business and customer discovery chapter.
- Demo video and final presentation.

## First steps this week

1. Install Revit, set up a C# add-in project, and display a message.
2. Download test models from the open sources above. [helpcenter.bimcollab](https://helpcenter.bimcollab.com/en/articles/325099-example-projects-and-templates)
3. Write a list of 20 candidate rules and group them into the five check types.
4. Draft an interview script and message 15 BIM professionals.
5. Agree on roles, the repository, and a weekly demo.

If you tell me which Revit version you have and your team's skills, I can draft the rule JSON schema and a week-by-week task list for each member.