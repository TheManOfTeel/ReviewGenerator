import { enableProdMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app/app.component';
import { ContactComponent } from './app/contact/contact.component';
import { HomeComponent } from './app/home/home.component';
import { environment } from './environments/environment';

export function getBaseUrl() {
  return document.getElementsByTagName('base')[0].href;
}

if (environment.production) {
  enableProdMode();
}

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    provideRouter([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'contact', component: ContactComponent }
    ])
  ]
})
  .catch(err => console.log(err));
