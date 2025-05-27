import { waitForAsync, ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FetchDataComponent } from './fetch-data.component';

describe('FetchDataComponent', () => {
  let fixture: ComponentFixture<FetchDataComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      declarations: [FetchDataComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FetchDataComponent);
    fixture.detectChanges();
  });

  it('should display loading', waitForAsync(() => {
    const titleText = fixture.nativeElement.querySelector('p').textContent;
    expect(titleText).toEqual('Loading...');
  }));
});
