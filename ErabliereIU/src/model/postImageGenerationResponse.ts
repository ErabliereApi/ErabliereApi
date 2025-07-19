export class PostImageGenerationResponse {
    constructor() {
        this.images = [];
    }

    images: PostImageGenerationResponseImage[];
}

export class PostImageGenerationResponseImage {
    constructor() {
        this.url = '';
    }

    url: string;
}