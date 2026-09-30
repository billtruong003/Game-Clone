# Google Play Data safety answers (Bruh Arrows, Nah Blocks, Meh Merge)

Shared by all three games. Developer: Bill The Dev. Prepared 2026-09-29 (ad stack: Google AdMob + Unity Ads).

Basis: the games have no accounts and no analytics of our own; progress/settings live only in PlayerPrefs on the device.
Everything declared below comes from the ad SDKs: the **Google Mobile Ads SDK** (Unity plugin 11.5.0, Android
`play-services-ads` 25.4.0) as the mediation platform, and **Unity Ads** (`com.unity3d.ads:unity-ads` 4.20.1) served through
the AdMob Unity Ads adapter (`com.google.ads.mediation:unity` 4.20.1.0, Unity package `com.google.ads.mobile.mediation.unity` 3.21.1).
Consent uses Google's UMP SDK (`user-messaging-platform` 4.0.0). Google Play counts data collected by third-party SDKs inside
the app as collected by the app, so it must be declared.

Definitions used (Play Console Help, source 3):
- **Collected** = transmitted off the device. Data that stays on the device (PlayerPrefs) is not "collected".
- **Shared** = transferred to a third party. Both SDK vendors declare their data as collected **and** shared (sources 1 and 2),
  so every row below is declared as shared.

The answers are the union of Google's disclosure (source 1) and Unity Ads' disclosure (source 2): if either SDK collects a type,
it is declared, and the purposes are the combined list.

## Section 1: Data collection and security (top-level questions)

| Question | Answer | Notes |
|---|---|---|
| Does your app collect or share any of the required user data types? | **Yes** | Via ad SDKs only. |
| Is all of the user data collected by your app encrypted in transit? | **Yes** | Google Mobile Ads SDK: all data encrypted in transit with TLS (source 1). Unity Ads: "encrypted in transit: Yes" (source 2). |
| Which of the following methods of account creation does your app support? | **My app does not allow users to create an account** | No login of any kind. |
| Do you provide a way for users to request that their data is deleted? | **No** (we hold no user data on our servers) | Unity Ads answers Yes for its SDK: players can use the Data Privacy icon in a Unity ad to opt out and request deletion (source 2, source 5). Google offers no SDK-level deletion; users reset/delete the advertising ID (source 1). Optional: answer Yes and point to the contact email, forwarding requests to Google/Unity. Deletion URL is only mandatory for apps with accounts. TODO decide. |
| Independent security review | No | Optional badge. |

## Section 2: Data types

Only the rows below are declared. Everything else (name, email, address, phone, other personal info, credit info, card/bank
numbers, health, messages, photos/videos, audio, files, calendar, contacts, precise location, web browsing, in-app search
history, installed apps, user-generated content, crash logs, other performance data) = **not collected**. Neither SDK lists
crash logs or precise location (sources 1 and 2).

| Play category → data type | Collected | Shared | Processed ephemerally? | Required or optional | Purposes (collected = shared) | Collected by |
|---|---|---|---|---|---|---|
| Location → **Approximate location** | Yes | Yes | No | Required | Advertising or marketing, Analytics, App functionality, Fraud prevention/security & compliance | Google (IP address used to estimate general location); Unity Ads (approximate location) |
| Personal info → **User IDs** | Yes | Yes | No | Required | App functionality | Unity Ads lists "Personal identifiers" for App functionality (source 2). TODO verify: the games set no user ID of their own; Unity's row likely covers its installation/session IDs. Kept declared because Unity's official answer is Yes. |
| Financial info → **Purchase history** | Yes | Yes | No | Required | Advertising or marketing, Analytics | Unity Ads (source 2). TODO verify: Unity's docs say in-app purchase information is only collected when certain Ads SDK features are enabled; the games do not send purchase events to Unity Ads. Declared conservatively because the games have an IAP ("Remove ads"). Remove only if confirmed. |
| App activity → **App interactions** | Yes | Yes | No | Required | Advertising or marketing, Analytics, Fraud prevention/security & compliance | Google (app launches, taps, video views); Unity Ads (page views and taps **inside the ad only**, not gameplay) |
| App info and performance → **Diagnostics** | Yes | Yes | No | Required | Advertising or marketing, Analytics, App functionality, Fraud prevention/security & compliance | Google (app launch time, hang rate, energy usage); Unity Ads (app diagnostics: App functionality, Analytics) |
| Device or other IDs → **Device or other IDs** | Yes | Yes | No | Required (see note) | Advertising or marketing, Analytics, App functionality, Fraud prevention/security & compliance | Google (Android advertising ID, app set ID); Unity Ads (device identifiers incl. advertising ID, installation ID) |

Notes on the table:
- **Required vs optional:** players cannot switch off ad-SDK collection inside the app (they can only decline personalization
  via the UMP form in consent regions, reset/delete the advertising ID in Android settings, or buy Remove ads, which stops
  interstitials but rewarded ads and the SDKs remain). Google marks only the advertising ID as optional and Unity marks
  everything required, so "Required" is the conservative answer. Do not mix answers per region.
- **Other app activity (Unity "app usage times"):** not declared. Unity says it is only collected when *Acquire Optimization*
  is enabled in the Unity Monetization dashboard (source 2). TODO verify that setting is off for the three games' Unity Ads
  projects; if it is on, add App activity → Other actions with Advertising or marketing, Analytics, Fraud prevention/security & compliance.
- **Personalization** purpose is not ticked: the games do not personalize content. Ad personalization is covered by "Advertising or marketing".
- **Device information** (model, OS version, language, screen, carrier) has no separate Play data type; neither vendor lists it
  as its own item. It is covered by the policy text and Diagnostics/Device IDs.
- **Google Play Billing / Unity IAP:** the payment itself is handled by Google Play, which Play's guidance exempts. TODO verify:
  Unity IAP (com.unity.purchasing 5.4.3) does not send transaction/receipt data to Unity servers in our configuration
  (Unity Services/Analytics are disabled in ProjectSettings/UnityConnectSettings.asset). Purchase history is already declared above
  because of Unity Ads, so the only change would be adding App functionality as a purpose.
- **Google Play In-App Review:** nothing to declare; the developer receives no data from it.
- **Mediated networks:** declared for Google AdMob + Unity Ads only (the only adapter in `Assets/Plugins/Android/mainTemplate.gradle`).
  Any extra network added later needs its own data-safety disclosure reviewed and the privacy policy updated.

## Section 3: Related Play Console declarations (App content)

| Declaration | Answer |
|---|---|
| Ads | **Yes, my app contains ads** |
| Advertising ID | **Yes**, uses advertising ID. Purposes: Advertising or marketing, Analytics, Fraud prevention/security & compliance. TODO verify the merged manifest of a release AAB contains `com.google.android.gms.permission.AD_ID` (declared by `play-services-ads`). |
| Target audience and content | **13-15, 16-17, 18+** only. Do not select any under-13 group (keeps the app out of the Families policy; the games do not set child-directed or under-age-of-consent tags). Appeal to children: **No**. |
| Privacy policy URL | Hosted copy of `Store/privacy/<game>.html` (one per game). TODO fill in once hosted. |
| Data deletion | Not required (no accounts). |
| Government / financial / health / news app | No |

## Section 4: Consent recap (for reviewers and for the policy)

- Consent regions (EEA/UK/Switzerland): `AdMobAdProvider` calls `ConsentInformation.Update` and
  `ConsentForm.LoadAndShowConsentFormIfRequired` (Google UMP) and only initializes `MobileAds` once `ConsentInformation.CanRequestAds()`
  is true. Players can reopen the form in Settings → Privacy options (`Privacy.ShowOptions` → `ConsentForm.ShowPrivacyOptionsForm`),
  shown when `PrivacyOptionsRequirementStatus.Required`.
- Unity Ads consent: the Unity Ads adapter (3.20.0+, we ship 3.21.1) forwards GDPR consent from IAB TCF CMPs such as UMP (source 4).
  TODO verify in the AdMob UI that **Unity Ads is on the GDPR and US state regulations ad partners lists** (Privacy & messaging), as Google requires (source 4).
- US state privacy laws: TODO verify whether a US state regulations message is published in AdMob Privacy & messaging. Google does not
  apply those settings to mediated networks; for Unity Ads the game would need `UnityAds.SetConsentMetaData("privacy.consent", ...)`
  before ad requests (source 4), which the code does not call today. If neither is done, the policy's opt-out instructions (reset
  advertising ID) are the only mechanism.

## Sources

1. Google Mobile Ads SDK, Google Play data disclosure: https://developers.google.com/admob/android/privacy/play-data-disclosure
   (IP address → approximate location; user product interactions incl. app launches, taps, video views; diagnostic information incl.
   launch time, hang rate, energy usage; device and account identifiers incl. Android advertising ID and app set ID; purposes: advertising,
   analytics, fraud prevention; ad ID optional via manifest; all data encrypted in transit with TLS.)
2. Unity Ads, Google Play data safety section: https://docs.unity.com/en-us/grow/ads/privacy/google-data-safety
   (collected and shared, not ephemeral, required: approximate location; personal identifiers; purchase history; page views and taps in the ad;
   other app activity only with Acquire Optimization; app diagnostics; device or other identifiers. Crash logs: No. Encrypted in transit: Yes.
   Deletion requests: Yes.)
3. Play Console Help, Provide information for Google Play's Data safety section: https://support.google.com/googleplay/android-developer/answer/10787469
4. Google AdMob, Unity Ads mediation (Unity plugin): https://developers.google.com/admob/unity/mediation/unity
   (sections "EU consent and GDPR", "US states privacy laws", and adding Unity Ads to the ad partners lists.)
5. Unity Game Player and App User Privacy Policy: https://unity.com/legal/game-player-and-app-user-privacy-policy
   (Data Privacy icon in the ad unit for opt-out and deletion; app interaction data tied to an installation ID kept up to 12 months.)
   General Unity Privacy Policy: https://unity.com/legal/privacy-policy
