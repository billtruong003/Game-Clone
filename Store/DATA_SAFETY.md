# Google Play Data safety answers (Arrow Out, Eye Blast, Eye Merge)

Shared by all three games. Developer: Bill The Dev. Prepared 2026-09-29.

Basis: the games have no accounts and no analytics of our own; progress/settings live only in PlayerPrefs on the device.
Everything declared below comes from the ad SDKs (AppLovin MAX + Google AdMob via MAX mediation). Google Play counts
data collected by third-party SDKs inside the app as collected by the app, so it must be declared.

Definitions used (Play Console Help, "Provide information for Google Play's Data safety section"):
- **Collected** = transmitted off the device. Data that stays on the device (PlayerPrefs) is not "collected".
- **Shared** = transferred to a third party. The ad SDKs send data to AppLovin/Google and their ad partners for
  advertising, so it is declared as shared (this matches Google's own disclosure for its SDK, which says it "collects and shares" these types).

## Section 1: Data collection and security (top-level questions)

| Question | Answer | Notes |
|---|---|---|
| Does your app collect or share any of the required user data types? | **Yes** | Via ad SDKs only. |
| Is all of the user data collected by your app encrypted in transit? | **Yes** | Google Mobile Ads SDK: all data encrypted in transit with TLS (source 1). AppLovin SDK uses HTTPS. TODO verify: AppLovin has no explicit public statement found; confirm no mediated adapter uses cleartext. |
| Which of the following methods of account creation does your app support? | **My app does not allow users to create an account** | No login of any kind. |
| Do you provide a way for users to request that their data is deleted? | **No** (we hold no user data on our servers) | Optional: answer Yes and point to the contact email, explaining requests are forwarded to AppLovin's privacy request portal. Deletion URL is only mandatory for apps with accounts. TODO decide. |
| Independent security review | No | Optional badge. |

## Section 2: Data types

Only the rows below are declared. Everything else (personal info, financial info, health, messages, photos/videos,
audio, files, calendar, contacts, precise location, web browsing, search history, installed apps) = **not collected**.

| Play category → data type | Collected | Shared | Processed ephemerally? | Required or optional | Purposes (collected) | Purposes (shared) | Collected by |
|---|---|---|---|---|---|---|---|
| Location → **Approximate location** | Yes | Yes | No | Required | Advertising or marketing, Analytics, Fraud prevention/security & compliance | Advertising or marketing, Analytics, Fraud prevention/security & compliance | Google (IP address used to estimate general location, source 1); AppLovin (IP, country, time zone, locale, source 3) |
| App activity → **App interactions** | Yes | Yes | No | Required | Advertising or marketing, Analytics | Advertising or marketing, Analytics | Google (app launches, taps, video views, source 1); AppLovin (ad views/clicks, event data, source 3) |
| App info and performance → **Diagnostics** | Yes | Yes | No | Required | Analytics (app/SDK performance monitoring) | Analytics | Google (launch time, hang rate, energy usage, source 1). TODO verify whether AppLovin also collects **Crash logs**; if so add App info and performance → Crash logs with the same answers. |
| Device or other IDs → **Device or other IDs** | Yes | Yes | No | Required (see note) | Advertising or marketing, Analytics, Fraud prevention/security & compliance | Advertising or marketing, Analytics, Fraud prevention/security & compliance | Google (Android advertising ID, app set ID, source 1); AppLovin (GAID, device identifiers, source 3) |

Notes on the table:
- **Required vs optional:** players cannot switch off ad-SDK collection inside the app (they can only decline
  personalization via the consent form in consent regions, reset/delete the advertising ID in Android settings, or
  buy Remove ads, which stops interstitials but rewarded ads and the SDK remain). "Required" is the conservative answer. TODO verify with the current
  Play guidance whether "optional" is acceptable for consent-region users; do not mix answers per region.
- **Personalization** purpose is not ticked: the games do not personalize content. Ad personalization is covered by "Advertising or marketing".
- **Device information** (model, OS version, language, screen, carrier) has no separate Play data type; Google does not list it as its own disclosure item. It is covered by the policy text and Diagnostics/Device IDs.
- **Purchase history (Financial info):** not declared. The "Remove ads" purchase is processed by Google Play Billing; we do not
  receive payment details and the entitlement is stored locally. Play's guidance exempts data Google Play itself collects for billing.
  TODO verify: Unity IAP (com.unity.purchasing 5.4.3) does not send transaction/receipt data to Unity servers in our configuration
  (Unity Services/Analytics are disabled in ProjectSettings/UnityConnectSettings.asset). If it does, declare Financial info → Purchase history (collected, App functionality).
- **Google Play In-App Review:** nothing to declare; the developer receives no data from it.
- **Mediated networks:** declared for AppLovin + Google AdMob only. TODO verify the final adapter list in the MAX Integration Manager
  (Assets/MaxSdk/Mediation currently has no adapters checked in). Every extra network (e.g. Unity Ads, Meta, Mintegral, ironSource)
  must have its own data-safety disclosure reviewed and the privacy policy updated.

## Section 3: Related Play Console declarations (App content)

| Declaration | Answer |
|---|---|
| Ads | **Yes, my app contains ads** |
| Advertising ID | **Yes**, uses advertising ID. Purposes: Advertising or marketing, Analytics, Fraud prevention/security & compliance. TODO verify the merged manifest of a release AAB contains `com.google.android.gms.permission.AD_ID` (added by the Play services ads-identifier dependency). |
| Target audience and content | **13-15, 16-17, 18+** only. Do not select any under-13 group (keeps the app out of the Families policy; AppLovin must not be used for users who are children, source 4). Appeal to children: **No**. |
| Privacy policy URL | Hosted copy of `Store/privacy/<game>.html` (one per game). TODO fill in once hosted. |
| Data deletion | Not required (no accounts). |
| Government / financial / health / news app | No |

## Section 4: Consent recap (for reviewers and for the policy)

- Consent regions (EEA/UK/Switzerland and others AppLovin flags as `ConsentFlowUserGeography.Gdpr`): Google UMP form
  shown by AppLovin's Terms & Privacy Policy flow before the first ad request. Players can reopen it in Settings → Privacy options
  (`Privacy.ShowOptions` → `MaxSdk.CmpService.ShowCmpForExistingUser`).
- US state privacy laws: TODO verify whether the MAX consent flow is configured to show the US state-regulations message (CCPA/CPRA
  "do not sell or share") in the Build Switcher / AppLovin dashboard. If not, the policy's opt-out instructions (reset advertising ID) are the only mechanism.

## Sources

1. Google Mobile Ads SDK, Google Play data disclosure: https://developers.google.com/admob/android/privacy/play-data-disclosure
   (data types: IP address → approximate location; user product interactions; diagnostic information; device and account identifiers
   incl. Android advertising ID and app set ID; purposes: advertising, analytics, fraud prevention; TLS in transit.)
2. Play Console Help, Provide information for Google Play's Data safety section: https://support.google.com/googleplay/android-developer/answer/10787469
3. AppLovin Privacy Policy: https://legal.applovin.com/privacy/ (also https://www.applovin.com/privacy/) (device data incl. model, OS, carrier,
   IP, advertising IDs; app/event interaction data; country, time zone, locale; retention "often ... shorter than two (2) years" or until deletion is requested;
   privacy request portal https://www.applovin.com/en/privacy-data-request)
4. AppLovin MAX Android privacy integration guide: https://support.applovin.com/en/max/android/overview/privacy
   (list AppLovin in the privacy policy; UMP automation; do not initialize the SDK for users who are children.)

Note: AppLovin does not appear to publish a dedicated Google Play Data safety mapping page (searched 2026-09-29; the public
GitHub request for one, AppLovin-MAX-SDK-Android issue #198, is no longer reachable). The AppLovin rows above are therefore mapped
by us from its privacy policy. TODO verify against AppLovin support or the dashboard if they publish one later.
