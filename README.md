# YTMDv2-Amuse-Fix
Small application which proxies 6klabs Amuse service and injects code to make it work with YTMD v2

# IMPORTANT NOTES
- This program does not automatically update! You are expected to come back to this repository perodically to check for updates or changes
- Amuse will most likely not automatically update with setting changes from their dashboard. You need to click "Refresh cache of current page" in the browser source properties to correct this

# Usage
To get started using this program, first grab the latest release at https://github.com/NovusTheory/YTMDv2-Amuse-Fix/releases/tag/latest.

### Obtain your Amuse Browser Source Url

![image](https://github.com/user-attachments/assets/b6c017bf-cffa-4f47-b80d-4242bc322b6c)

Your source url is going to look something like `https://6klabs.com/widget/youtube/5bdf09ea2e1c9f89654dafeb89f5cd91aa27e4bcda3b8556f74fc64846aa13d0`

You will want to replace `https://6klabs.com` with `http://localhost:9963` such that your url now looks like `http://localhost:9963/widget/youtube/5bdf09ea2e1c9f89654dafeb89f5cd91aa27e4bcda3b8556f74fc64846aa13d0`

### Create a new Browser source in OBS

![image](https://github.com/user-attachments/assets/c422e8cc-0735-4a5e-93d2-63a3a08f96b8)

![image](https://github.com/user-attachments/assets/db633daf-7a99-43fc-9ea1-acae562e6b49)

Then make sure the URL is set to your modified url

![image](https://github.com/user-attachments/assets/3a279483-9da6-4ab7-9382-8d718522c782)

Follow the normal procedures of authorizing a companion in YTMDesktop refreshing the browser source page cache in OBS if needed to reinitiate the authorization

Amuse should now be in your OBS and working properly with YTMDesktop v2 as long as this application is running in the background :)
