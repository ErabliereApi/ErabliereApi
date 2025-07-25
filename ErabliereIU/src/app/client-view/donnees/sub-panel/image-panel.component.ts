
import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ErabliereApi } from 'src/core/erabliereapi.service';
import { GetImageInfo } from 'src/model/imageInfo';

@Component({
    selector: 'image-panel',
    templateUrl: "./image-panel.component.html",
    styles: [`
        
    `],
    imports: []
})
export class ImagePanelComponent implements OnInit {
  
    constructor(private readonly api: ErabliereApi, private readonly route: ActivatedRoute) {

    }

    @Input() idErabliereSelectionnee: any;
    images: GetImageInfo[] = [];
    selectedImageMetadata: GetImageInfo = new GetImageInfo();
    azureImageAPIInfo: string = "";
    selectedImage?: string;
    take: number = 2;
    skip: number = 0;
    search: string = "";
    modalTake: number = 1;
    modalSkip: number = 0;
    modalHasNext: boolean = true;
    modalTitle: string = "Image";

    ngOnInit(): void {
        this.route.params.subscribe(params => {
            this.idErabliereSelectionnee = params['idErabliereSelectionee'];
            this.fetchImages();
        });
        this.imageInterval = setInterval(() => {
            this.fetchImages();
        }, 1000 * 60 * 10);
    }

    ngOnDestroy() {
        clearInterval(this.imageInterval);
    }

    imageInterval: any;

    fetchImages() {
        console.log("fetch image for", this.idErabliereSelectionnee);
        this.api.getImages(
                this.idErabliereSelectionnee, this.take, this.skip, this.search)
            .then(images => {
                this.images = images;
            });
    }
    
    nextImage() {
        console.log("next image");
        this.take = 2;
        this.skip += this.take;
        this.fetchImages();
    }
        
    previousImage() {
        console.log("previous image");
        this.take = 2;
        this.skip -= this.take;
        if (this.skip < 0) {
            this.skip = 0;
        }
        this.fetchImages();
    }

    fetchModalImages() {
        console.log("fetch image for", this.idErabliereSelectionnee);
        this.api.getImages(this.idErabliereSelectionnee, this.modalTake, this.modalSkip, this.search).then(images => {
            if (images.length > 0) {
                this.selectedImage = images[0].images;
                this.selectedImageMetadata = images[0];
                this.azureImageAPIInfo = JSON.stringify(JSON.parse(images[0].azureImageAPIInfo ?? ""), null, 2);
                this.modalHasNext = true;
                this.modalTitle = images[0].name ?? "Image";
            }
            else {
                this.modalHasNext = false;
                this.modalSkip -= this.modalTake;
            }
        });
    }

    modalNextImage() {
        console.log("modal next image");
        this.modalTake = 1;
        this.modalSkip += this.modalTake;
        this.fetchModalImages();
    }
        
    modalPreviousImage() {
        console.log("modal previous image");
        this.modalTake = 1;
        this.modalSkip -= this.modalTake;
        if (this.modalSkip < 0) {
            this.modalSkip = 0;
        }
        this.fetchModalImages();
    }

    selectImage(image: GetImageInfo, localSkip: number) {
        this.selectedImage = image.images;
        this.selectedImageMetadata = image;
        this.azureImageAPIInfo = JSON.stringify(JSON.parse(image.azureImageAPIInfo ?? ""), null, 2);
        this.modalTake = 1;
        this.modalSkip = this.skip + localSkip;
        if (this.modalSkip < 0) {
            this.modalSkip = 0;
        }
        this.modalTitle = image.name ?? "Image";
    }

    imgOnKeyUp(event: KeyboardEvent, image: GetImageInfo, localSkip: number) {
        if (event.key == "Enter") {
            this.selectImage(image, localSkip);
        }
    }

    searchFromInput(event: any) {
        console.log("searchFromInput", event);
        const search = event.target.value;
        this.search = search;
        console.log("search", search);
        this.fetchImages();
    }
}